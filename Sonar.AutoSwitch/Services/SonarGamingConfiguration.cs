namespace Sonar.AutoSwitch.Services;

public record SonarGamingConfiguration(string? Id, string Name)
{
    // Return empty so AutoCompleteBox shows its watermark when no config is selected.
    public override string ToString() => Id is null ? "" : Name;
}

// Outcome of the most recent attempt to talk to Sonar, surfaced as a header status dot.
public enum SonarConnectionStatus
{
    Idle,         // no switch attempted yet this session
    Connected,    // last interaction with Sonar succeeded
    Disconnected, // last interaction failed — Sonar not reachable
}