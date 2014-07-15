package com.studiogobo.fi.spatialmatchmaking.model.requirements;

import com.studiogobo.fi.spatialmatchmaking.model.ClientRecord;
import org.codehaus.jackson.annotate.JsonTypeName;

@JsonTypeName("requireLocationWithin")
public class RequireLocationWithin extends Requirement {
    public int radius;

    @Override
    public boolean Evaluate(ClientRecord us, ClientRecord them) {
        return us.location.Distance(them.location) <= radius;
    }
}
