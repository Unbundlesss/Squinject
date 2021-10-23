using System.Collections.Generic;

namespace Squinject
{
    namespace PresetData
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        public class Tuning
        {
            public double startValue { get; set; }
            public double defaultValue { get; set; }
            public List<double> multipliers { get; set; }
        }

        public class Reverb
        {
            public double startValue { get; set; }
            public double defaultValue { get; set; }
            public List<double> multipliers { get; set; }
        }

        public class ModSustain
        {
            public double startValue { get; set; }
            public double defaultValue { get; set; }
            public List<double> multipliers { get; set; }
        }

        public class Pitch
        {
            public double startValue { get; set; }
            public double defaultValue { get; set; }
            public List<double> multipliers { get; set; }
        }

        public class Levels
        {
            public double startValue { get; set; }
            public double defaultValue { get; set; }
            public List<double> multipliers { get; set; }
        }

        public class AmpAttack
        {
            public double startValue { get; set; }
            public double defaultValue { get; set; }
            public List<double> multipliers { get; set; }
        }

        public class AmpSustain
        {
            public double startValue { get; set; }
            public double defaultValue { get; set; }
            public List<double> multipliers { get; set; }
        }

        public class AmpDecay
        {
            public double startValue { get; set; }
            public double defaultValue { get; set; }
            public List<double> multipliers { get; set; }
        }

        public class ModDecay
        {
            public double startValue { get; set; }
            public double defaultValue { get; set; }
            public List<double> multipliers { get; set; }
        }

        public class FilterCutoff
        {
            public double startValue { get; set; }
            public double defaultValue { get; set; }
            public List<double> multipliers { get; set; }
        }

        public class FilterEnvAmount
        {
            public double startValue { get; set; }
            public double defaultValue { get; set; }
            public List<double> multipliers { get; set; }
        }

        public class ModAttack
        {
            public double startValue { get; set; }
            public double defaultValue { get; set; }
            public List<double> multipliers { get; set; }
        }

        public class FilterResonance
        {
            public double startValue { get; set; }
            public double defaultValue { get; set; }
            public List<double> multipliers { get; set; }
        }

        public class ModRelease
        {
            public double startValue { get; set; }
            public double defaultValue { get; set; }
            public List<double> multipliers { get; set; }
        }

        public class AmpRelease
        {
            public double startValue { get; set; }
            public double defaultValue { get; set; }
            public List<double> multipliers { get; set; }
        }

        public class PitchEnv
        {
            public double startValue { get; set; }
            public double defaultValue { get; set; }
            public List<double> multipliers { get; set; }
        }

        public class Params
        {
            public Tuning tuning { get; set; }
            public Reverb reverb { get; set; }
            public ModSustain modSustain { get; set; }
            public Pitch pitch { get; set; }
            public Levels levels { get; set; }
            public AmpAttack ampAttack { get; set; }
            public AmpSustain ampSustain { get; set; }
            public AmpDecay ampDecay { get; set; }
            public ModDecay modDecay { get; set; }
            public FilterCutoff filterCutoff { get; set; }
            public FilterEnvAmount filterEnvAmount { get; set; }
            public ModAttack modAttack { get; set; }
            public FilterResonance filterResonance { get; set; }
            public ModRelease modRelease { get; set; }
            public AmpRelease ampRelease { get; set; }
            public PitchEnv pitchEnv { get; set; }
        }

        public class Root
        {
            public string interaction { get; set; }
            public int inputMode { get; set; }
            public Params @params { get; set; }
            public List<string> samples { get; set; }
            public double tune { get; set; }
            public double loopStart { get; set; }
            public double filterPitchAmount { get; set; }
            public List<int> macroMappings { get; set; }
            public List<string> macroNames { get; set; }
            public double loopLength { get; set; }
            public string user_id { get; set; }
            public string packName { get; set; }
            public List<string> triggerIcons { get; set; }
            public string packType { get; set; }
            public string engineType { get; set; }
            public int filterType { get; set; }
            public List<string> productIds { get; set; }
            public List<double> macroDefaults { get; set; }
            public long created { get; set; }
            public string icon { get; set; }
            public List<string> tags { get; set; }
            public int app_version { get; set; }
            public string author { get; set; }
            public bool loopingMode { get; set; }
            public string description { get; set; }
            public string engine { get; set; }
            public int packSortOrder { get; set; }
            public string colour { get; set; }
            public string name { get; set; }
            public string type { get; set; }
        }

    }
}
