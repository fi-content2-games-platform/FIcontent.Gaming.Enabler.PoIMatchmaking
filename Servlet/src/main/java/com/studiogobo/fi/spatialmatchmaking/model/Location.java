package com.studiogobo.fi.spatialmatchmaking.model;

public class Location
{
    public double longitude;
    public double latitude;

    //returns the distance in meters from one Location to another
    public double Distance(Location other)
    {
        // This is the "haversine" algorithm, which apparently has good error propagation characteristics

        double lon1 = Math.toRadians(longitude);
        double lon2 = Math.toRadians(other.longitude);
        double lat1 = Math.toRadians(latitude);
        double lat2 = Math.toRadians(other.latitude);

        double dLat = lat2 - lat1;
        double dLon = lon2 - lon1;

        // a is the square of half the straight-line distance between the points, on a unit sphere
        double a = Math.sin(dLat/2) * Math.sin(dLat/2) +
            Math.sin(dLon/2) * Math.sin(dLon/2) * Math.cos(lat1) * Math.cos(lat2);

        // c is the arc angle between the points
        double c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));

        // d is the arc length on a sphere of radius R
        double R = 6371000; // m
        double d = R * c;

        return d;
    }
}
