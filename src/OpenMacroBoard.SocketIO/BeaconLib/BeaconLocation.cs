using System;
using System.Net;

#pragma warning disable AV1505 // Namespace should match with assembly name

namespace BeaconLib;

/// <summary>
/// Class that represents a discovered beacon
/// </summary>
internal sealed class BeaconLocation
{
    public BeaconLocation(IPEndPoint address, string data, DateTimeOffset lastAdvertised)
    {
        Address = address;
        Data = data;
        LastAdvertised = lastAdvertised;
    }

    public IPEndPoint Address { get; }
    public string Data { get; }
    public DateTimeOffset LastAdvertised { get; }

    public override string ToString()
    {
        return Data;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals(Address, ((BeaconLocation)obj).Address);
    }

    public override int GetHashCode()
    {
        return Address?.GetHashCode() ?? 0;
    }
}
