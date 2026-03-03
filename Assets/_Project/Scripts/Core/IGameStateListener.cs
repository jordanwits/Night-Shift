namespace NightShift.Core
{
    /// <summary>
    /// Interface for systems that react to game state changes.
    /// </summary>
    public interface IGameStateListener
    {
        void OnGameStateEntered(GameState state);
        void OnGameStateExited(GameState state);
    }
}
