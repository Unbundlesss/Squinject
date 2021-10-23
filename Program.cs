using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

using Couchbase.Lite;

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

    class Program
    {
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
                Console.WriteLine( "Please run Squinject <path_to_endlesss_studio_root_dir>\n" );
                Environment.Exit( -1 );
            }

            var packImageInstallDir = Path.Combine( args[0], @"Assets\Images\Packs\" );
            if ( !Directory.Exists( packImageInstallDir ) )
            {
                Console.WriteLine( $"Unable to find Endlesss Studio image pack directory :\n{packImageInstallDir}\n" );
                Environment.Exit( -1 );
            }

            var endlesssProdDataDir = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), @"Endlesss\production\Data\" );
            if ( !Directory.Exists( endlesssProdDataDir ) )
            {
                Console.WriteLine( $"Could not find Endlesss Studio data directory :\n{endlesssProdDataDir}\n" );
                Environment.Exit( -1 );
            }
            var sampleInstallDir = Path.Combine( endlesssProdDataDir, @".attachments\760ed7e4f390c1aa9df9fc5854622147\" );
            if ( !Directory.Exists( sampleInstallDir ) )
            {
                Console.WriteLine( $"Could not find Endlesss Studio sample cache :\n{sampleInstallDir}\n" );
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
                Console.WriteLine( $"Error :\n{ex.Message}\n" );
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
                presetNameToDocumentId.Add( ao.Document.Properties["name"] as string, ao.Document.Id );
            }

            var queryClips = database.GetExistingView( "clips" );
            queryClips.UpdateIndex();
            foreach ( var ao in queryClips.CreateQuery().Run() )
            {
                var ff = ao.Document.Properties["cdn_attachments"] as Newtonsoft.Json.Linq.JObject;
                var fx = ff["clips/ogg"];

                clipHashToDocumentId.Add( fx["hash"].ToStringValueType(), ao.Document.Id );
            }

            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            foreach ( var lp in allPresets )
            {
                var dataFlat = df.Execute( lp.Data );

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
                    Console.WriteLine( $"Instrument already registered : {lp.Name,-18} : {presetNameToDocumentId[lp.Name]}" );

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
                        clipData.Add( "created", unixTimestamp );
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

                    Console.WriteLine( $"Copy [{inputSamplePaths[sI]}] => [{outputFile}]" );

                    File.Copy( inputSamplePaths[sI], outputFile, true );
                }
            }
        }

    }
}
