namespace NightShift.Core
{
    /// <summary>
    /// Interface for systems that react to instability threshold crossings.
    /// </summary>
    public interface IInstabilityListener
    {
        void OnInstabilityThresholdCrossed(int tier);
    }
}
