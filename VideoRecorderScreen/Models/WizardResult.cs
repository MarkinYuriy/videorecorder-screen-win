using System.Windows;

namespace VideoRecorderScreen.Models
{
    public class WizardResult
    {
        public required Rect Region { get; init; }
        public int Fps { get; init; }
        public bool MicEnabled { get; init; }
        public bool SystemAudioEnabled { get; init; }
    }
}
