package com.studiogobo.fi.spatialmatchmaking.servlet;

import com.studiogobo.fi.spatialmatchmaking.model.*;
import com.studiogobo.fi.spatialmatchmaking.model.requirements.Requirement;
import org.codehaus.jackson.map.ObjectMapper;
import org.codehaus.jackson.map.type.CollectionType;
import org.codehaus.jackson.map.type.TypeFactory;
import org.codehaus.jettison.json.JSONException;
import org.codehaus.jettison.json.JSONObject;

import javax.ws.rs.*;
import javax.ws.rs.core.Context;
import javax.ws.rs.core.Response;
import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URI;
import java.net.URL;
import java.util.Arrays;
import java.util.Iterator;
import java.util.UUID;
import java.util.Vector;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;

@Path("/")
public class Servlet
{
    private void Log(String message)
    {
        System.out.println("SRV: " + message);
    }

    public void preDestroy() throws InterruptedException
    {
        jobQueue.Quit();
    }

    @GET
    @Path("clients/{id}")
    @Produces("application/json")
    public ClientRecord getClient(@PathParam("id") int id)
    {
        Log("GET " + id);
        ServletClientRecord record = getServletClientRecord(id);

        Log("    OK");
        return record.clientRecord;
    }

    @POST
    @Path("clients")
    @Produces("application/json")
    @Consumes("application/json")
    public Response createClient(JSONObject data) throws IOException, JSONException, InterruptedException {

        final int id = lastClientId.incrementAndGet();

        ClientRecord record = new ClientRecord(id);

        updateClient(record, data);

        clientData.put(id, new ServletClientRecord(record));

        jobQueue.Enqueue(new Runnable() {
            public void run()
            {
                matchmaker.UpdateClient(id);
            }
        });

        Log("POST => " + id);

        return Response.created(URI.create("" + id)).entity(record).build();
    }

    private final Object lock = new Object();
    void updateClient(ClientRecord client, JSONObject data) throws JSONException {

        // If we return an error state then we shouldn't modify any data, so we gather up the changes
        // here and only apply them when the request is fully parsed
        UUID newUuid = null;
        Vector<Requirement> newRequirements = null;
        String newConnectionInfo = null;
        Location newLocation = null;
        PoiSearchOptions poiOptions = null;

        Iterator it = data.keys();
        while (it.hasNext()) {
            String key = (String) it.next();

            if (key.equals("uuid")) {
                // The value should be a string representing an UUID
                // It must be unique.

                UUID uuid;

                try {
                    uuid = UUID.fromString(data.getString(key));
                } catch (JSONException e) {
                    Log("    error: " + e.toString());
                    e.printStackTrace();
                    throw new WebApplicationException(Response.Status.BAD_REQUEST);
                }

                boolean ok = true;
                for (ServletClientRecord scr : clientData.values()) {
                    if (scr.clientRecord != client && scr.clientRecord.uuid.equals(uuid)) {
                        ok = false;
                        break;
                    }
                }
                if (!ok)
                   throw new WebApplicationException(Response.Status.CONFLICT);

                 newUuid = uuid;
            } else if (key.equals("requirements")) {
                // The value should be a JSON array of Requirement objects.
                //
                // Each object should contain a "@type" field, e.g. "requireNotUuid",
                // along with whatever other data the specific requirement class
                // accepts.
                ObjectMapper mapper = new ObjectMapper();
                TypeFactory factory = mapper.getTypeFactory();
                CollectionType type = factory.constructCollectionType(Vector.class, Requirement.class);
                try {
                    newRequirements = mapper.readValue(data.getJSONArray(key).toString(), type);
                } catch (IOException e) {
                    Log("    error: " + e.toString());
                    e.printStackTrace();
                    throw new WebApplicationException(Response.Status.BAD_REQUEST);
                } catch (JSONException e) {
                    Log("    error: " + e.toString());
                    e.printStackTrace();
                    throw new WebApplicationException(Response.Status.BAD_REQUEST);
                }
            } else if (key.equals("connectionInfo")) {
                newConnectionInfo = data.getString(key);
            } else if (key.equals("location")) {
                try {
                    JSONObject jsonObject = data.getJSONObject(key);
                    newLocation = new Location();
                    newLocation.latitude = jsonObject.getDouble("latitude");
                    newLocation.longitude = jsonObject.getDouble("longitude");

                } catch (JSONException e) {
                    Log("    error: " + e.toString());
                    e.printStackTrace();
                    throw new WebApplicationException(Response.Status.BAD_REQUEST);
                }
            } else if (key.equals("poiSearchOptions")) {
                JSONObject jsonObject = data.getJSONObject(key);
                poiOptions = new PoiSearchOptions();
                poiOptions.snapRadius = jsonObject.getInt("snapRadius");
                poiOptions.maxSearchRadius = jsonObject.getInt("maxSearchRadius");
                poiOptions.maxPoisReturned = jsonObject.getInt("maxPoisReturned");

                //make sure the url doesn't end with a backslash
                String url = jsonObject.getString("poiGeUrl");
                if (url.endsWith("/")) {
                    url = url.substring(0, url.length() - 1);
                }
                poiOptions.poiGeUrl = url;
            } else {
                Log("    unknown field: " + key);
                throw new WebApplicationException(Response.Status.BAD_REQUEST);
            }
        }

            if (newUuid != null)
                client.uuid = newUuid;
            if (newRequirements != null)
                client.requirements = newRequirements;
            if (newConnectionInfo != null)
                client.connectionInfo = newConnectionInfo;

            //set the client's location. Depends on "location" and "poiSearchOptions"
            if (newLocation != null && poiOptions != null)//we've updated both the location of the user and their poi search options
            {
                client.poiSearchOptions = poiOptions;
                client.SetNearestPOI(newLocation);

                if (client.nearestPOI != null && client.nearestPOI.distToClient < poiOptions.snapRadius) {
                    newLocation = client.nearestPOI.loc;
                    Log("updated client's location to nearest POI, lat:" + newLocation.latitude + ", lon: " + newLocation.longitude);
                }
                client.location = newLocation;
            } else if (newLocation != null) //&& poiOptions == null. We've updated the location of the user but not their poi search options
            {
                client.SetNearestPOI(newLocation);

                if (client.nearestPOI != null && client.nearestPOI.distToClient < poiOptions.snapRadius) {
                    newLocation = client.nearestPOI.loc;
                    Log("updated client's location to nearest POI, lat:" + newLocation.latitude + ", lon: " + newLocation.longitude);
                }
                client.location = newLocation;
            } else if (poiOptions != null) //&& newLocation == null. We've updated the poi search options of the user but not their location
            {
                client.poiSearchOptions = poiOptions;
                client.SetNearestPOI(client.location);

                if (client.nearestPOI != null && client.nearestPOI.distToClient < poiOptions.snapRadius) {
                    newLocation = client.nearestPOI.loc;
                    Log("updated client's location to nearest POI, lat:" + newLocation.latitude + ", lon: " + newLocation.longitude);
                }
                client.location = newLocation;
            }

    }


    //get the nearest POI to this client. If none found, returns empty JSON Object {}
    @GET
    @Path("clients/{id}/nearestPOI")
    @Produces("application/json")
    public Response getNearestPOI (@PathParam("id") int id) throws IOException, JSONException
    {
        JSONObject returned = new JSONObject();
        ServletClientRecord client = clientData.get(id);

        if (client != null && client.clientRecord.nearestPOI != null) //client exists and has a nearestPOI
        {

            URL myURL = new URL(client.clientRecord.poiSearchOptions.poiGeUrl+"/get_pois.php?poi_id=" + client.clientRecord.nearestPOI.uuid);

            HttpURLConnection con =  (HttpURLConnection)  myURL.openConnection();
            con.setRequestProperty("Accept-Language", "en");
            //have to set "accept-language" header, otherwise this produces an error in bbox_search.php, line 145

            BufferedReader in = new BufferedReader(new InputStreamReader(con.getInputStream()));
            String inputLine;
            StringBuffer response = new StringBuffer();

            while ((inputLine = in.readLine()) != null) {
                response.append(inputLine);
            }
            in.close();
            con.disconnect();

            returned = new JSONObject(response.toString());
        }

        return Response.ok().entity(returned).build();

    }


    @PUT
    @Path("clients/{id}")
    public Response updateClient(@PathParam("id") final int id, JSONObject data) throws IOException, JSONException, InterruptedException {
        ServletClientRecord client = getServletClientRecord(id);

        Log("updating client " + id);
        // update client from request
        updateClient(client.clientRecord, data);

        if (client.match_id != 0)
        {
            final int match_id = client.match_id;
            jobQueue.Enqueue(new Runnable() {
                @Override
                public void run() {
                    matchmaker.VerifyMatch(match_id);
                }
            });
        }

        jobQueue.Enqueue(new Runnable() {
            public void run()
            {
                matchmaker.UpdateClient(id);
            }
        });

        return Response.noContent().build();
    }

    @DELETE
    @Path("clients/{id}")
    public Response deleteClient(@PathParam("id") final int id, @QueryParam("removeFromMatch") final boolean removeFromMatch) throws InterruptedException {

        jobQueue.Enqueue(new Runnable() {
            public void run()
            {
                matchmaker.DeleteClient(id, removeFromMatch);
            }
        });

        Log("DELETE " + id + " => accepted");
        return Response.status(202).build();
    }

    @GET
    @Path("matches")
    @Produces("application/json")
    public Response seekMatch(@QueryParam("client") final int client_id, @QueryParam("numMatched") final int numClientsMatched,
                              @Context javax.ws.rs.core.UriInfo uriInfo)
            throws InterruptedException
    {
        Log("    path " + uriInfo.getAbsolutePath());
        Log("match query for " + client_id);

        ServletClientRecord client = getServletClientRecord(client_id);

        if (client.waitUntilMatched.getCount() > 0)
        {
            if (!client.active)
            {
                client.active = true;
                jobQueue.Enqueue(new Runnable() {
                    public void run()
                    {
                        matchmaker.UpdateClient(client_id);
                    }
                });
            }

            Log("    waiting for match...");
        }

        if (client.match_id != 0 && numClientsMatched != 0)//ie, a value has been included as a query parameter.
        //a host, which is already in a match, is looking for changes in the match.
        {
            MatchRecord match = getMatch(client.match_id);
            if (match.clients.length == numClientsMatched) //no change to the match record. Wait again
                client.ResetWait();
        }

        boolean awaitResult = client.waitUntilMatched.await(30, TimeUnit.SECONDS);
        client.active = false;

        if (!awaitResult)
        {
            return Response.status(204).header("Retry-After", "1").build();
        }

        Log("    match id " + client.match_id);
        URI uri = uriInfo.getAbsolutePathBuilder().path("" + client.match_id).build();
        Log("    returning redirect to " + uri.toString());

        return Response.seeOther(uri).entity(client.clientRecord).build();
    }

    @GET
    @Path("matches/{id}")
    @Produces("application/json")
    public MatchRecord getMatch(@PathParam("id") int id)
    {
        MatchRecord record = matchmaker.GetMatchRecord(id);
        if (record == null)
        {
            Log("GET " + id + " => not found");

            throw new WebApplicationException(Response.Status.NOT_FOUND);
        }

        Log("GET " + id + " => " + Arrays.toString(record.clients));

        return record;
    }

    @GET
    @Path("state")
    @Produces("application/json")
    public JSONObject getState() throws JSONException {
        int numClients = 0;
        int numClientsPendingDelete = 0;
        int numUnmatchedClients = 0;
        int numMatches = matchmaker.NumMatches();

        for (ServletClientRecord client : clientData.values())
        {
            ++numClients;
            if (client.match_id == 0)
                ++numUnmatchedClients;
            if (client.deleted)
                ++numClientsPendingDelete;
        }

        JSONObject result = new JSONObject();
        result.put("numClients", numClients);
        result.put("numClientsPendingDelete", numClientsPendingDelete);
        result.put("numUnmatchedClients", numUnmatchedClients);
        result.put("numMatches", numMatches);
        return result;
    }

    @POST
    @Path("test")
    @Produces("application/json")
    @Consumes("application/json")
    public JSONObject test(JSONObject object) throws JSONException {
        JSONObject result = new JSONObject();
        result.put("hello", object.optString("first"));
        result.put("world", object.optString("second"));
        return result;
    }

    @POST
    @Path("clients/{id}/update")
    public Response updateClient2(@PathParam("id") int id, JSONObject data) throws IOException, JSONException, InterruptedException {
        return updateClient(id, data);
    }

    @POST
    @Path("clients/{id}/delete")
    public Response deleteClient2(@PathParam("id") int id, @QueryParam("removeFromMatch") final boolean removeFromMatch) throws InterruptedException {
        return deleteClient(id, removeFromMatch);
    }

    @GET
    @Path("crossdomain.xml")
    @Produces("application/xml")
    public String crossDomainXml()
    {
        return "<?xml version=\"1.0\"?>\n" +
                "<cross-domain-policy>\n" +
                "<allow-access-from domain=\"*\"/>\n" +
                "</cross-domain-policy>";
    }

    private ServletClientRecord getServletClientRecord(int client_id) {
        ServletClientRecord client = clientData.get(client_id);

        if (client == null)
        {
            Log("    client " + client_id + " not found");

            throw new WebApplicationException(Response.Status.NOT_FOUND);
        }

        client.KeepAlive();

        return client;
    }

    private ConcurrentHashMap<Integer, ServletClientRecord> clientData = new ConcurrentHashMap<Integer, ServletClientRecord>();
    private AtomicInteger lastClientId = new AtomicInteger();

    private JobQueue jobQueue = new JobQueue(10);

    private Matchmaker matchmaker = new Matchmaker(clientData);
}
