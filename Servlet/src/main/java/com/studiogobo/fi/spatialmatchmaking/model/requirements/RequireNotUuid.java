package com.studiogobo.fi.spatialmatchmaking.model.requirements;

import com.studiogobo.fi.spatialmatchmaking.model.ClientRecord;
import org.codehaus.jackson.annotate.JsonTypeName;

import java.util.UUID;

@JsonTypeName("requireNotUuid")
public class RequireNotUuid extends Requirement
{
    public UUID uuid;

    @Override
    public boolean Evaluate(ClientRecord us, ClientRecord them)
    {
        return !them.uuid.equals(uuid);
    }
}
