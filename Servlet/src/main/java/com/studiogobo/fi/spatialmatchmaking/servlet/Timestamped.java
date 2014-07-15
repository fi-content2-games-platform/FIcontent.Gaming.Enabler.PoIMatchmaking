package com.studiogobo.fi.spatialmatchmaking.servlet;

public class Timestamped
{
    private long updateTimeMillis;

    public Timestamped()
    {
        KeepAlive();
    }

    public void KeepAlive()
    {
        updateTimeMillis = System.currentTimeMillis();
    }

    public long AgeMillis()
    {
        return System.currentTimeMillis() - updateTimeMillis;
    }
}
