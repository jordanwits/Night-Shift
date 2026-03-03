namespace NightShift.Core
{
    /// <summary>
    /// Static proxy for player state. Allows Systems to query downed status
    /// without referencing the Player assembly.
    /// Updated by PlayerVitals.
    /// </summary>
    public static class PlayerStateProxy
    {
        public static bool IsDowned { get; set; }
    }
}
