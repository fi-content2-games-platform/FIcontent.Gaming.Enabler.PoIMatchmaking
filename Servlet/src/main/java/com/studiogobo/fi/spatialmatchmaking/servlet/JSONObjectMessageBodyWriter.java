package com.studiogobo.fi.spatialmatchmaking.servlet;

import org.codehaus.jettison.json.JSONObject;

import javax.ws.rs.Produces;
import javax.ws.rs.core.MediaType;
import javax.ws.rs.core.MultivaluedMap;
import javax.ws.rs.ext.MessageBodyWriter;
import javax.ws.rs.ext.Provider;
import java.io.IOException;
import java.io.OutputStream;
import java.lang.annotation.Annotation;
import java.lang.reflect.Type;

@Provider
@Produces("application/json")
public class JSONObjectMessageBodyWriter implements MessageBodyWriter<JSONObject> {
    @Override
    public long getSize(JSONObject obj, Class type, Type genericType,
                        Annotation[] annotations, MediaType mediaType) {
        return -1;
    }

    @Override
    public boolean isWriteable(Class type, Type genericType,
                               Annotation annotations[], MediaType mediaType) {
        return type == JSONObject.class;
    }

    @Override
    public void writeTo(JSONObject target, Class type, Type genericType,
                        Annotation[] annotations, MediaType mediaType,
                        MultivaluedMap httpHeaders, OutputStream outputStream)
            throws IOException {

        outputStream.write(target.toString().getBytes("UTF-8"));
    }
}
