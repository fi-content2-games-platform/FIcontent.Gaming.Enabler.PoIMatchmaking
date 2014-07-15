package com.studiogobo.fi.spatialmatchmaking.model;

import com.studiogobo.fi.spatialmatchmaking.model.requirements.Requirement;
import org.codehaus.jettison.json.JSONException;
import org.codehaus.jettison.json.JSONObject;

import javax.ws.rs.WebApplicationException;
import javax.ws.rs.core.Response;
import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.MalformedURLException;
import java.net.URL;
import java.util.Iterator;
import java.util.List;
import java.util.UUID;
import java.util.Vector;

public class ClientRecord
{
    public int id;

    public UUID uuid = new UUID(0, 0);

    public List<Requirement> requirements = new Vector<Requirement>();

    public Location location = new Location();

    public String connectionInfo;

    public PoiSearchOptions poiSearchOptions = new PoiSearchOptions();

    public POI nearestPOI = null;

    public ClientRecord()
    {
        this(0);
    }

    public ClientRecord(int _id)
    {
        id = _id;
    }

    public boolean RequirementsPass(ClientRecord other)
    {
        for (Requirement req : requirements)
        {
            if (!req.Evaluate(this, other))
            {
                return false;
            }
        }
        return true;
    }

    private void Log(String s)
    {
        System.out.println("POI: " + s);
    }

    //when we first call this method, use snapRadius for searchRadius. This searchRadius is increased every time we
    // don't find any matches until it's value is greater than maxSearchRadius
    public void SetNearestPOI(Location clientLoc)
    {
        SetNearestPOIWithinRadius(clientLoc, poiSearchOptions.snapRadius);
    }


    private boolean triedMaxSearchRadius = false;
    private void SetNearestPOIWithinRadius(Location clientLoc, int searchRadius)
    {
        if (searchRadius > poiSearchOptions.maxSearchRadius)
        {
            if (!triedMaxSearchRadius)
            {
                searchRadius = poiSearchOptions.maxSearchRadius;
                triedMaxSearchRadius = true;
            }
            else {
                nearestPOI = null;
                Log("Could not find any nearby POIs");
                return;
            }
        }

        double clientLat = clientLoc.latitude;
        double clientLong = clientLoc.longitude;

        double minDist = searchRadius;

        try {

            URL myURL = new URL(poiSearchOptions.poiGeUrl+"/radial_search.php?lat="+clientLat+"&lon="+clientLong+"&radius="+searchRadius+"&max_results="+poiSearchOptions.maxPoisReturned);

            HttpURLConnection con =  (HttpURLConnection)  myURL.openConnection();
            con.setRequestProperty("Accept-Language", "en");
            //have to set "accept-language" header, otherwise this produces an error in bbox_search.php, line 145


            BufferedReader in = new BufferedReader(new InputStreamReader(con.getInputStream()));
            //The moment you call getInputStream(), an HTTP get is sent to the target server.
            String inputLine;
            StringBuffer response = new StringBuffer();

            while ((inputLine = in.readLine()) != null) {
                response.append(inputLine);
            }
            in.close();
            con.disconnect();

            JSONObject json = new JSONObject(response.toString());
            String jsonClass = json.get("pois").getClass().toString();

            if (jsonClass.contains("JSONArray"))//array is only used when no POIs were found
            {
                //increase radius by 500m and try again.
                SetNearestPOIWithinRadius(clientLoc, searchRadius + 500);
            }
            else //at least one POI was found
            {
                POI currentPOI = new POI();

                JSONObject pois = (JSONObject) json.get("pois");
                Iterator it = pois.keys();

                //loop through all POIs near the user and find the one that is the closest to the user, in a straight line
                Log("All nearby POIs found:");
                while (it.hasNext())
                {
                    String poiUuid = (String)it.next();

                    JSONObject data = pois.getJSONObject(poiUuid);
                    data = data.getJSONObject("fw_core");

                    //lat/long of poi
                    JSONObject location = data.getJSONObject("location");
                    location = location.getJSONObject("wgs84");
                    Location poiLoc = new Location();
                    poiLoc.latitude = location.getDouble("latitude");
                    poiLoc.longitude = location.getDouble("longitude");

                    //name of poi
                    data = data.getJSONObject("name");
                    String name = data.getString("");

                    double currentDist = clientLoc.Distance(poiLoc);
                    if (currentDist < minDist) //update nearestPOI
                    {
                        minDist = currentDist;

                        currentPOI.uuid = poiUuid;
                        currentPOI.loc = poiLoc;
                        currentPOI.distToClient = (int) Math.round(currentDist);
                        currentPOI.name = name;

                    }
                    //print all found POIs
                    Log(name + "\tlat: " + poiLoc.latitude + "\tlong: " + poiLoc.longitude + "\tdist: " + currentDist + "m");
                }

                nearestPOI = currentPOI;
                Log("Nearest POI: " +currentPOI.name +" lat: " +  currentPOI.loc.latitude + ", lon:" +  currentPOI.loc.longitude);
            }
            System.out.println("");

        }
        catch (MalformedURLException e)//if there is no protocol for the url
        {
            //at the moment, I don't think Unity is able to read the error message, only the error code
            Log("Error: invalid url," + e.toString());
            throw new WebApplicationException(Response.status(Response.Status.BAD_REQUEST).entity(e.toString()).build());
        }
        catch (IOException e) //if the file radial_search.php could not be found
        {
            Log("Error: " + e.toString());
            throw new WebApplicationException(Response.status(Response.Status.BAD_REQUEST).entity(e.toString()).build());
        }
        catch (JSONException e)
        {
            Log("Error parsing the response " + e.toString());
            throw new WebApplicationException(Response.status(Response.Status.BAD_REQUEST).entity(e.toString()).build());
        }
    }

}
