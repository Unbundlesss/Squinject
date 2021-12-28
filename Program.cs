using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Couchbase.Lite;

using Microsoft.Win32;

using Newtonsoft.Json;

namespace Squinject
{
    public class LoadedPreset
    {
        public string Name;
        public string SamplePath;
        public PresetData.Root Data;

        public static string CalculateMD5( string filename )
        {
            if ( !File.Exists(filename) )
            {
                throw new FileNotFoundException( $"Cannot load sample [{filename}]" );
            }
            using ( var md5 = MD5.Create() )
            {
                using ( var stream = File.OpenRead( filename ) )
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString( hash ).Replace( "-", "" ).ToLowerInvariant();
                }
            }
        }
    }

    public static class RegistryAccess
    {
        // Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Endlesss Ltd\Endlesss | InstallationPath
        public static string GetEndlesssStudioInstallDir()
        {
            try
            {
                using ( RegistryKey key = Registry.LocalMachine.OpenSubKey( "SOFTWARE\\Endlesss Ltd\\Endlesss" ) )
                {
                    if ( key != null )
                    {
                        Object o = key.GetValue("InstallationPath");
                        if ( o != null )
                        {
                            return (o as String);
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                Console.WriteLine( "Unable to read Studio installation directory\n" );
                Console.WriteLine( ex.Message );
                Environment.Exit( -1 );
            }
            return null;
        }
    }


    class Program
    {
        static string packImageInstallDir;
        static string endlesssProdDataDir;
        static string sampleInstallDir;

        static bool verboseMode = false;

        static void Main( string[] args )
        {
            var squonkRoot = @"output";

            // try to check we're basically in the right place
            if ( !Directory.Exists( Path.Combine( squonkRoot, "_cache" ) ) )
            {
                Console.WriteLine( "Can't find a workable Squonker \\output directory. Run Squinject next to Squonker! honK\n" );
                Environment.Exit( -1 );
            }

            if ( args.Length < 1 )
            {
                Console.WriteLine( "Please run Squinject with either :\n" );
                Console.WriteLine( "    squinject.exe studio\n" );
                Console.WriteLine( "    squinject.exe ios\n" );
                Environment.Exit( -1 );
            }

            if ( args[0].ToLower() == "studio" )
            {
                var installDir = RegistryAccess.GetEndlesssStudioInstallDir();
                Console.WriteLine( $"Using Endlesss Studio install dir : {installDir}\n" );

                packImageInstallDir = Path.Combine( installDir, @"Assets\Images\Packs\" );
                if ( !Directory.Exists( packImageInstallDir ) )
                {
                    Console.WriteLine( $"Unable to find Endlesss Studio image pack directory :\n{packImageInstallDir}\n" );
                    Environment.Exit( -1 );
                }

                endlesssProdDataDir = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), @"Endlesss\production\Data\" );
                if ( !Directory.Exists( endlesssProdDataDir ) )
                {
                    Console.WriteLine( $"Could not find Endlesss Studio data directory :\n{endlesssProdDataDir}\n" );
                    Environment.Exit( -1 );
                }
                sampleInstallDir = Path.Combine( endlesssProdDataDir, @".attachments\760ed7e4f390c1aa9df9fc5854622147\" );
                if ( !Directory.Exists( sampleInstallDir ) )
                {
                    Console.WriteLine( $"Could not find Endlesss Studio sample cache :\n{sampleInstallDir}\n" );
                    Environment.Exit( -1 );
                }
            }
            else if ( args[0].ToLower() == "ios" )
            {
                var squonkTransientDir = "injection_db";

                packImageInstallDir = Path.Combine( squonkTransientDir, @"_packs\" );
                Directory.CreateDirectory( packImageInstallDir );
                
                endlesssProdDataDir = squonkTransientDir;

                sampleInstallDir = Path.Combine( squonkTransientDir, @"_samples\" );
                Directory.CreateDirectory( sampleInstallDir );
            }
            else
            {
                Console.WriteLine( $"unknown operation mode '{args[0]}'\n" );
                Environment.Exit( -1 );
            }


            // go connect to the CBL db
            Couchbase.Lite.Storage.SystemSQLite.Plugin.Register();
            Manager manager = new Manager(new DirectoryInfo(endlesssProdDataDir), new ManagerOptions() { });
            Database database = manager.GetDatabase("presets");

            try
            {
                foreach ( var packImg in Directory.GetFiles( squonkRoot, "*.jpg" ) )
                {
                    var imageName = new FileInfo(packImg).Name;
                    File.Copy( packImg, Path.Combine( packImageInstallDir, imageName ), true );
                }

                List< LoadedPreset > allPresets = new List<LoadedPreset>();

                foreach ( var pack in Directory.EnumerateDirectories( squonkRoot ) )
                {
                    if ( pack.Contains( "_cache" ) )
                        continue;

                    foreach ( var preset in Directory.EnumerateDirectories( pack ) )
                    {
                        var presetName = preset.Replace(pack, "").Substring(1);

                        var presetDataText =File.ReadAllText( Path.Combine(preset, $"{presetName}.json") );
                        var presetData = JsonConvert.DeserializeObject<PresetData.Root>(presetDataText);

                        LoadedPreset lp = new LoadedPreset()
                        {
                            Name = presetName,
                            SamplePath = Path.Combine(preset, "samples"),
                            Data = presetData
                        };

                        allPresets.Add( lp );
                    }

                }

                InjectPresets( database, sampleInstallDir, allPresets );
            }
            catch ( Exception ex )
            {
                var st = new StackTrace(ex, true);
                if ( st.FrameCount >= 1 )
                {
                    var frame = st.GetFrame(0);
                    Console.WriteLine( $"Error :\n{ex.Message}\n{ex.StackTrace}\n{frame.GetFileName()} @ {frame.GetFileLineNumber()}\n\n" );
                }
                else
                {
                    Console.WriteLine( $"Error :\n{ex.Message}\n{ex.StackTrace}\n\n" );
                }
            }
            finally
            {
                database.Close();
            }
        }

        private static void InjectPresets( Database database, string sampleInstallDir, List<LoadedPreset> allPresets )
        {
            var df = new DataFlatten();

            Dictionary<string, string> presetNameToDocumentId = new Dictionary<string, string>(1024);
            Dictionary<string, string> clipHashToDocumentId = new Dictionary<string, string>(1024);

            var queryPresets = database.GetExistingView( "presets" );
            queryPresets.UpdateIndex();
            foreach ( var ao in queryPresets.CreateQuery().Run() )
            {
                var presetName = ao.Document.Properties["name"] as string;

                if ( presetNameToDocumentId.ContainsKey( presetName ) )
                {
                    throw new Exception( $"Duplicate preset? [{presetName}] when building presetNameToDocumentId table" );
                }
                presetNameToDocumentId.Add( presetName, ao.Document.Id );
            }

            var queryClips = database.GetExistingView( "clips" );
            queryClips.UpdateIndex();
            foreach ( var ao in queryClips.CreateQuery().Run() )
            {
                var ff = ao.Document.Properties["cdn_attachments"] as Newtonsoft.Json.Linq.JObject;
                var fx = ff["clips/ogg"];

                var oggHash = fx["hash"].ToStringValueType();

                if ( clipHashToDocumentId.ContainsKey( oggHash ) )
                {
                    throw new Exception( $"Duplicate OGG hash value (probably duplicate sample?) [{oggHash}] " );
                }

                clipHashToDocumentId.Add( fx["hash"].ToStringValueType(), ao.Document.Id );
            }

            Int64 unixTimestampNano = (Int64)((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds) * 1000;

            foreach ( var lp in allPresets )
            {
                var dataFlat = df.Execute( lp.Data );

                // bring the 'created' up to date as this seems to be what is used to order the packs in the app toolbar
                {
                    object forwarded = (object)unixTimestampNano;
                    dataFlat["created"] = forwarded;
                }

                List<string> inputSamplePaths = new List<string>();
                List<string> inputSampleMD5s  = new List<string>();
                List<long>   inputSampleSizes = new List<long>();

                if ( lp.Data.packType == "Drums" )
                {
                    foreach ( var smpl in lp.Data.samples )
                    {
                        var inputSample     = Path.Combine( lp.SamplePath, $"{smpl}.ogg" );
                        var inputSampleMD5  = LoadedPreset.CalculateMD5( inputSample );
                        var inputSampleSize = (new FileInfo( inputSample )).Length;

                        inputSamplePaths.Add( inputSample );
                        inputSampleMD5s.Add( inputSampleMD5 );
                        inputSampleSizes.Add( inputSampleSize );
                    }
                }
                else
                {
                    var inputSample     = Path.Combine( lp.SamplePath, $"{lp.Name}.ogg" );
                    var inputSampleMD5  = LoadedPreset.CalculateMD5( inputSample );
                    var inputSampleSize = (new FileInfo( inputSample )).Length;

                    inputSamplePaths.Add( inputSample );
                    inputSampleMD5s.Add( inputSampleMD5 );
                    inputSampleSizes.Add( inputSampleSize );
                }


                Document instDoc;
                if ( presetNameToDocumentId.ContainsKey( lp.Name ) )
                {
                    Console.WriteLine( $"Updating existing Instrument : {lp.Name,-18} : {presetNameToDocumentId[lp.Name]}" );

                    instDoc = database.GetExistingDocument( presetNameToDocumentId[lp.Name] );

                    // copy clips data back from current doc
                    dataFlat["clips"] = instDoc.Properties["clips"];
                }
                else
                {
                    instDoc = database.CreateDocument();

                    Console.WriteLine( $"Creating new instrument record : {lp.Name,-18} : {instDoc.Id}" );

                    List<string> createdSampleDocuments = new List<string>();
                    for ( int sI = 0; sI < inputSamplePaths.Count; sI ++ )
                    {
                        var smpMD5 = inputSampleMD5s[sI];

                        var cdnData = new Dictionary<string, object>();
                        cdnData.Add( "endpoint", "ishanisv.org" );
                        cdnData.Add( "hash", smpMD5 );
                        cdnData.Add( "key", $"squonker/{smpMD5}" );
                        cdnData.Add( "length", inputSampleSizes[sI] );
                        cdnData.Add( "mime", "audio/ogg" );
                        cdnData.Add( "url", $"http://ishanisv.org/squonker/{smpMD5}" );

                        var oggData = new Dictionary<string, object>();
                        oggData.Add( "clips/ogg", cdnData );

                        var clipData = new Dictionary<string, object>();
                        clipData.Add( "app_version", 4321 );
                        clipData.Add( "created", unixTimestampNano );
                        clipData.Add( "midiNotePitch", 60 );
                        clipData.Add( "name", lp.Name );
                        clipData.Add( "cdn_attachments", oggData );
                        clipData.Add( "type", "ReaktorClip" );

                        var clipDoc = database.CreateDocument();

                        clipData.Add( "_rev", clipDoc.CurrentRevisionId );
                        clipDoc.PutProperties( clipData );

                        createdSampleDocuments.Add( clipDoc.Id );
                    }

                    dataFlat["clips"] = createdSampleDocuments.ToArray();
                }

                dataFlat.Add( "_rev", instDoc.CurrentRevisionId );
                dataFlat["type"] = "InstrumentPreset";
                
                instDoc.PutProperties( dataFlat );

                for ( int sI = 0; sI < inputSamplePaths.Count; sI++ )
                {
                    // update sample data, copy into cache as <MD5>.blob
                    var outputFile = Path.Combine(sampleInstallDir, $"{inputSampleMD5s[sI]}.blob");

                    if ( verboseMode )
                        Console.WriteLine( $"Copy [{inputSamplePaths[sI]}] => [{outputFile}]" );

                    File.Copy( inputSamplePaths[sI], outputFile, true );
                }
            }
        }

    }
}
