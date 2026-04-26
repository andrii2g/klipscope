namespace KlipScope.Core.Models;

public enum TransportKind
{
    Moonraker,
    KlipperTcp
}

public static class TransportKindExtensions
{
    public static string ToWireValue(this TransportKind transport) =>
        transport switch
        {
            TransportKind.Moonraker => "moonraker",
            TransportKind.KlipperTcp => "klipper-tcp",
            _ => "unknown"
        };
}
