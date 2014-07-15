package com.studiogobo.fi.spatialmatchmaking.servlet;

import com.studiogobo.fi.spatialmatchmaking.model.*;

import java.util.ArrayList;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicInteger;

public class Matchmaker
{
    private void Log(String message)
    {
        System.out.println(" MM: " + message);
    }

    public void UpdateClient(int id)
    {
        Log("UpdateClient(" + id + ")");

        ServletClientRecord primaryClientRecord = clientData.get(id);
        if (primaryClientRecord == null)
        {
            Log("client id " + id + " not found");
            return;
        }

        if (primaryClientRecord.match_id != 0)
        {
            Log("client id " + id + " already matched");
            return;
        }

        if (!primaryClientRecord.active)
        {
            Log("client id " + id + " is not active. Return");
            return;
        }

        final int wantedClients = 2;

        // Start with a list containing only the client we're trying to match
        ArrayList<ServletClientRecord> foundClients = new ArrayList<ServletClientRecord>();
        foundClients.add(primaryClientRecord);

        // Find compatible clients and add them to the list
        for (ServletClientRecord record : clientData.values())
        {

            //delete for now
//           // Expire clients who have been idle for a long time
//            if (record.AgeMillis() > 60000)
//            {
//                Log("client " + record.clientRecord.id +" has been idle for a long time. Delete");
//                DeleteClient(record.clientRecord.id, true);
//                continue;
//            }

//            // Ignore inactive clients
//            if (!record.active)
//                continue;


            // Ignore the client we're trying to match
            if (record.clientRecord.id == id)
                continue;

            // Ignore incompatible clients
            if (!primaryClientRecord.RequirementsPass(record) || !record.RequirementsPass(primaryClientRecord))
            {
                continue;
            }
            //get the match ID of record. If primaryClientRecord doesn't pass requirements of all clients in matchID, don't add to foundClients


            //ignore clients who already belong to a match session, and <id> does not pass the requirement for all those in the session
            if (record.match_id != 0) //ie, client we might match with, already belongs to a match session
            {
                boolean addRecord = true;
                MatchRecord match = matchData.get(record.match_id);
                for (int clientId : match.clients)
                {
                    ServletClientRecord recordMatches = clientData.get(clientId);
                    if (!primaryClientRecord.RequirementsPass(recordMatches) || !recordMatches.RequirementsPass(primaryClientRecord)) {
                        Log("    "+id+" does not pass the requirements of " +clientId + "(or vice versa)");
                        addRecord = false;
                    }
                }
                if (!addRecord)
                    continue;
            }

            foundClients.add(record);

            // Stop looking if we've found enough clients now.
            //
            // If finding compatible clients is a slow process, some may have quit by the time we finish.  In that
            // case, we should verify that the foundClients are still valid, in the loop, before breaking out.  So we
            // should be able to be quite confident that the client list is fairly reliable when we leave the loop.
            if (foundClients.size() == wantedClients)
                break;
        }

        if (foundClients.size() == wantedClients)
        {
            ServletClientRecord host = foundClients.get(foundClients.size() - 1);

            if (host.match_id == 0) //we are creating a new match record
            {
                // Make a list of client IDs
                int[] clientIdList = new int[foundClients.size()];
                for (int i = 0; i < foundClients.size(); ++i)
                    clientIdList[i] = foundClients.get(i).clientRecord.id;

                // Create a MatchRecord
                MatchRecord match = new MatchRecord(lastMatchId.incrementAndGet(), clientIdList);
                matchData.put(match.id, match);

                Log("        new match id " + match.id);

                // Mark these clients as part of the session.
                for (ServletClientRecord record : foundClients) {
                    Log("        client " + record.clientRecord.id);
                    record.match_id = match.id;

                    // Signal anybody watching the record to say that it has changed
                    record.waitUntilMatched.countDown();
                }
            }
            else //we are appending to a previously existing match record
            {
                Log("        reusing old match id " + host.match_id);

                MatchRecord existingMatch = matchData.get(host.match_id );
                //matchData.remove(host.match_id);

                int[] updatedClients = new int[existingMatch.clients.length + 1]; //the new array of clients
                for (int i = 0; i < updatedClients.length - 1; i++)
                    updatedClients[i] = existingMatch.clients[i]; //assign the values for the clients to the new array

                updatedClients[updatedClients.length - 1] = id; //add the current client to end of the list of clients
                existingMatch.clients = updatedClients;

                existingMatch.clients = updatedClients;
                matchData.replace(host.match_id, existingMatch);

                //mark the <id> client as part of  host.match_id session
                primaryClientRecord.match_id = host.match_id;

                for (int clientId : updatedClients)
                {
                    Log("        client " + clientId);
                    ServletClientRecord record = clientData.get(clientId);
                    record.waitUntilMatched.countDown(); // Signal anybody watching the record to say that it has changed
                }
            }
        }
    }

    //param: match id
    public void VerifyMatch(int id)
    {
        MatchRecord match = matchData.get(id);
        if (match == null)
            return;

        // Check all the clients are still compatible with the match, and cancel the match if any are unhappy
        for (int clientId : match.clients)
        {
            ServletClientRecord client = clientData.get(clientId);
            if (client == null)
            {
                if (match.clients.length == 2)
                    RemoveMatch(id);
                else
                    RemoveClientFromMatch(id,clientId);
                return;
            }

            for (int otherClientId : match.clients)
            {
                ServletClientRecord otherClient = clientData.get(otherClientId);

                if (!client.RequirementsPass(otherClient))
                {
                    if (match.clients.length == 2)
                        RemoveMatch(id);
                    else if (clientId == match.clients[0]) //don't delete the host, delete the other client instead
                        RemoveClientFromMatch(id, otherClientId);
                    else
                        RemoveClientFromMatch(id, clientId);
                }
           }
        }

    }

    //This is different from RemoveMatch() in that we don't delete the match record itself, just a client from the record
    public void RemoveClientFromMatch(int matchId, int clientId)
    {
        MatchRecord match = matchData.get(matchId);
        Log("    Remove client ("+ clientId + ") from match " + matchId);
        int[] newClients = new int[match.clients.length-1];

        int index = 0;
        for (int i = 0; i < match.clients.length; i++) {
            if (match.clients[i] != clientId)
            {
                newClients[index] = match.clients[i]; //keep all but the client we want to remove
                index++;
            }
        }
        match.clients = newClients;
        matchData.replace(matchId, match); //replace the old matchRecord, with the new one (which has one less client)

        // Remove the match reference from this client record
        ServletClientRecord client = clientData.get(clientId);
        if (client.match_id == matchId)
        {
            client.ClearMatch();
             if (client.deleted)
                clientData.remove(clientId);
        }

        UpdateClient(clientId); // Search again for match for this client, as it may be able to connect to different host/client
    }


    //remove the match record from the hash map
    public void RemoveMatch(int id)
    {
        Log("    Remove match " + id);
        MatchRecord match = matchData.remove(id);
        if (match == null)
            return;

        // Remove the match reference from the client records
        for (int clientId : match.clients)
        {
            ServletClientRecord client = clientData.get(clientId);
            if (client == null) continue;

            if (client.match_id == id)
            {
                client.ClearMatch();

                if (client.deleted) //I think with the new way that clients are not deleted upon connecting, client.deleted is always false
                    clientData.remove(clientId);
            }
        }

        // Search again for matches for these clients
        for (int clientId : match.clients)
        {
            UpdateClient(clientId);
        }
    }

    //removeFromMatch is set only to "true" after clicking the "quit" button in the Unity example.
    //if a client sets removeFromMatch=true, this will keep the match record in the map, deleting only the clients id from the clients array
    //deleting the host will delete the match record, regardless of removeFromMatch
    public void DeleteClient(int id, boolean removeFromMatch)
    {

        //clientData will not contain <id>, for example, when the host leaves the session, deleting the match record and clients in the process
        //a client of this previous session then quits the service, but as far as the code (and clientData) is concerned, it has already been deleted
        if (clientData.containsKey(id))
        {
            final ServletClientRecord client = clientData.get(id);
            client.deleted = true;
            if (client.match_id == 0)
            {
                clientData.remove(id);
            }
            else
            {
                MatchRecord match = GetMatchRecord(client.match_id);
                if (match == null) {
                    clientData.remove(id);
                    return;
                }

                boolean okToDelete = true;
                for (int clientId : match.clients) {
                    ServletClientRecord otherClient = clientData.get(clientId);
                    if (otherClient != null) {
                        if (!otherClient.deleted)
                            okToDelete = false;
                    }
                }

                if (okToDelete || match.clients[0] == id)
                {
                    for (int clientId : match.clients) {
                        clientData.remove(clientId);
                    }

                    RemoveMatch(client.match_id);
                }
                else if (removeFromMatch && matchData.get(client.match_id).clients.length > 1)
                    RemoveClientFromMatch(client.match_id, id);

            }
        }
    }

    public Matchmaker(ConcurrentHashMap<Integer, ServletClientRecord> data)
    {
        clientData = data;
    }

    public MatchRecord GetMatchRecord(int id) { return matchData.get(id); }
    public int NumMatches() { return matchData.size(); }

    private ConcurrentHashMap<Integer, ServletClientRecord> clientData;
    private ConcurrentHashMap<Integer, MatchRecord> matchData = new ConcurrentHashMap<Integer, MatchRecord>();
    private AtomicInteger lastMatchId = new AtomicInteger();

}
