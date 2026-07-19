namespace MyDutchBike.Parts
{
    /// <summary>Category a part belongs to, and the category a socket accepts. See docs/PART_SYSTEM_DESIGN.md.</summary>
    public enum PartCategory
    {
        None,
        Frame,
        Fork,
        Wheel,
        Tire,
        Crankset,
        Chain,
        Handlebar,
        Seat,
        Brake,
        Pedal,
    }
}
