package com.studiogobo.fi.spatialmatchmaking.model.requirements;

import com.studiogobo.fi.spatialmatchmaking.model.ClientRecord;
import org.codehaus.jackson.annotate.JsonTypeName;

import java.util.List;

@JsonTypeName("requireAttribute")
public class RequireAttribute extends Requirement
{
    public String attribute;
    public List<String> values;

    @Override
    public boolean Evaluate(ClientRecord us, ClientRecord them) {
        for (Requirement req : them.requirements)
        {
            if (!(req instanceof RequireAttribute))
                continue;

            RequireAttribute requireAttribute = (RequireAttribute)req;
            if (!requireAttribute.attribute.equals(attribute))
                continue;

            for (String ourValue : values)
            {
                for (String theirValue : requireAttribute.values)
                {
                    if (ourValue.equals(theirValue))
                        return true;
                }
            }
        }
        return false;
    }
}
