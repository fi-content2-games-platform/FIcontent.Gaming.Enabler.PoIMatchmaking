package com.studiogobo.fi.spatialmatchmaking.model.requirements;

import com.studiogobo.fi.spatialmatchmaking.model.ClientRecord;
import org.codehaus.jackson.annotate.JsonTypeName;

@JsonTypeName("requireLocationOverlap")
public class RequireLocationOverlap extends Requirement {
    public int radius;

    @Override
    public boolean Evaluate(ClientRecord us, ClientRecord them) {

        // Find the other client's RequireLocationOverlap requirement, if any, and get its radius;
        // if the other client doesn't specify this requirement then use 0 for the radius

        int otherRadius = 0;

        for (Requirement req : them.requirements)
        {
            if (!(req instanceof RequireLocationOverlap))
                continue;

            otherRadius = ((RequireLocationOverlap)req).radius;
            break;
        }

        // Pass if the total distance between clients is less than the sum of the radii
        return us.location.Distance(them.location) < radius + otherRadius;
    }
}
