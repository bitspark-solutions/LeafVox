namespace LeafVox.Tts;

/// <summary>Built-in registry of free, openly-licensed voices (all URLs + sizes verified against the sherpa-onnx tts-models release on 2026-10-23).</summary>
public static class VoiceCatalog
{
    private const string Base = "https://github.com/k2-fsa/sherpa-onnx/releases/download/tts-models/";

    public static IReadOnlyList<VoiceInfo> All { get; } = new[]
    {
        new VoiceInfo("en_US-lessac-medium", "Lessac (en-US)", "English (US)", VoiceTier.Light, ModelKind.Piper, Base + "vits-piper-en_US-lessac-medium.tar.bz2", 67_230_653),
        new VoiceInfo("en_US-ryan-high", "Ryan (en-US, high)", "English (US)", VoiceTier.Light, ModelKind.Piper, Base + "vits-piper-en_US-ryan-high.tar.bz2", 115_630_708),
        new VoiceInfo("en_US-amy-medium", "Amy (en-US)", "English (US)", VoiceTier.Light, ModelKind.Piper, Base + "vits-piper-en_US-amy-medium.tar.bz2", 67_223_746),
        new VoiceInfo("en_GB-alba-medium", "Alba (en-GB)", "English (UK)", VoiceTier.Light, ModelKind.Piper, Base + "vits-piper-en_GB-alba-medium.tar.bz2", 67_212_349),
        new VoiceInfo("en_GB-cori-high", "Cori (en-GB, high)", "English (UK)", VoiceTier.Light, ModelKind.Piper, Base + "vits-piper-en_GB-cori-high.tar.bz2", 115_574_061),
        new VoiceInfo("de_DE-thorsten-medium", "Thorsten (de)", "German", VoiceTier.Light, ModelKind.Piper, Base + "vits-piper-de_DE-thorsten-medium.tar.bz2", 67_214_254),
        new VoiceInfo("fr_FR-siwis-medium", "Siwis (fr)", "French", VoiceTier.Light, ModelKind.Piper, Base + "vits-piper-fr_FR-siwis-medium.tar.bz2", 67_207_459),
        new VoiceInfo("es_ES-sharvard-medium", "Sharvard (es)", "Spanish", VoiceTier.Light, ModelKind.Piper, Base + "vits-piper-es_ES-sharvard-medium.tar.bz2", 80_318_184),
        new VoiceInfo("hi_IN-rohan-medium", "Rohan (hi)", "Hindi", VoiceTier.Light, ModelKind.Piper, Base + "vits-piper-hi_IN-rohan-medium.tar.bz2", 67_221_139),
        new VoiceInfo("ar_JO-kareem-medium", "Kareem (ar)", "Arabic", VoiceTier.Light, ModelKind.Piper, Base + "vits-piper-ar_JO-kareem-medium.tar.bz2", 67_177_830),
        new VoiceInfo("bn-multi-low", "Bengali multi-speaker", "Bangla", VoiceTier.Light, ModelKind.Mimic3, Base + "vits-mimic3-bn-multi_low.tar.bz2", 79_886_227, SpeakerCount: 2),
        new VoiceInfo("kokoro-en-v0_19", "Kokoro (en, 11 voices)", "English", VoiceTier.Natural, ModelKind.Kokoro, Base + "kokoro-en-v0_19.tar.bz2", 319_625_534, SpeakerCount: 11),
        new VoiceInfo("kokoro-int8-multi-lang-v1_1", "Kokoro (25 languages, int8)", "Multilingual", VoiceTier.Natural, ModelKind.Kokoro, Base + "kokoro-int8-multi-lang-v1_1.tar.bz2", 147_031_220, SpeakerCount: 103),
        new VoiceInfo("kokoro-multi-lang-v1_1", "Kokoro (25 languages)", "Multilingual", VoiceTier.Natural, ModelKind.Kokoro, Base + "kokoro-multi-lang-v1_1.tar.bz2", 364_816_464, SpeakerCount: 103),
    };

    public static VoiceInfo? Find(string id) => All.FirstOrDefault(v => v.Id == id);

    public static VoiceInfo Default => Find("en_US-lessac-medium")!;
}
