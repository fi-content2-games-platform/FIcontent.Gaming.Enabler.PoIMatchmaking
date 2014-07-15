package com.studiogobo.fi.spatialmatchmaking.servlet;

import com.studiogobo.fi.spatialmatchmaking.model.ClientRecord;
import org.codehaus.jackson.annotate.JsonSubTypes;
import org.codehaus.jackson.annotate.JsonTypeInfo;
import org.codehaus.jackson.map.ObjectMapper;
import org.codehaus.jackson.map.ObjectWriter;
import org.codehaus.jackson.map.type.TypeFactory;
import org.junit.Test;
import org.junit.runner.RunWith;
import org.junit.runners.JUnit4;

import java.util.List;
import java.util.Vector;


@JsonTypeInfo(use= JsonTypeInfo.Id.NAME, include= JsonTypeInfo.As.PROPERTY, property="@type")
@JsonSubTypes({@JsonSubTypes.Type(TestClass.class), @JsonSubTypes.Type(TestClass2.class)})
abstract class TestBase
{
}

class TestClass extends TestBase
{
    public int number;
}

class TestClass2 extends TestBase
{
    public String string;
}

class Container
{
    public Vector<TestBase> elems;
}

@RunWith(JUnit4.class)
public class ServletTest {

    @Test
    public void testTest() throws Exception
    {
        String data;

        {
            TestClass tc = new TestClass();
            tc.number = 3;

            TestClass2 tc2 = new TestClass2();
            tc2.string = "Hello World";

            Container cont = new Container();
            cont.elems = new Vector<TestBase>();
            cont.elems.add(tc);
            cont.elems.add(tc2);

            ObjectMapper mapper = new ObjectMapper();
            data = mapper.writeValueAsString(cont);
            System.out.println(data);
        }

        {
            ObjectMapper mapper = new ObjectMapper();
            Container cont = mapper.readValue(data, Container.class);
            System.out.println(((TestClass)cont.elems.get(0)).number);
            System.out.println(((TestClass2)cont.elems.get(1)).string);
        }

        {
            TestClass tc = new TestClass();
            tc.number = 3;

            TestClass2 tc2 = new TestClass2();
            tc2.string = "Hello World";

            Container cont = new Container();
            cont.elems = new Vector<TestBase>();
            cont.elems.add(tc);
            cont.elems.add(tc2);

            ObjectMapper mapper = new ObjectMapper();
            TypeFactory factory = mapper.getTypeFactory();
            ObjectWriter writer = mapper.typedWriter(factory.constructCollectionType(List.class, TestBase.class));
            data = writer.writeValueAsString(cont.elems);
            System.out.println(data);


            List<TestBase> l = mapper.readValue(data, factory.constructCollectionType(List.class, TestBase.class));
            System.out.println(((TestClass)l.get(0)).number);
            System.out.println(((TestClass2)l.get(1)).string);
        }
    }

    @Test
    public void testModel() throws Exception
    {
        ClientRecord record = new ClientRecord();
        ObjectMapper mapper = new ObjectMapper();
        String data = mapper.writeValueAsString(record);
        System.out.println(data);
    }
}
