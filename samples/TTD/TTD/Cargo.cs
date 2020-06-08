namespace TTD
{
    public class Cargo
    {
        public Cargo()
        {}

        public Cargo(int cargoId, Location origin, Location destination)
        {
            CargoId = cargoId;
            Origin = origin;
            Destination = destination;
        }
        public int CargoId { get; set; }
        public Location Destination { get; }
        public Location Origin { get; }

    }

}
