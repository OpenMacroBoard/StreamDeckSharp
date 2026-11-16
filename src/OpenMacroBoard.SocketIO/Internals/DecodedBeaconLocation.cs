using BeaconLib;

namespace OpenMacroBoard.SocketIO.Internals;

internal class DecodedBeaconLocation
{
    public DecodedBeaconLocation(BeaconLocation beaconLocation, SocketIODeviceMetaData metaData)
    {
        BeaconLocation = beaconLocation;
        MetaData = metaData;
    }

    public BeaconLocation BeaconLocation { get; }
    public SocketIODeviceMetaData MetaData { get; }
}
