using System.Xml.Serialization;

namespace KmaProxy.Models;

[XmlRoot(ElementName="static")]
public class Static { 

    [XmlAttribute(AttributeName="href")] 
    public string Href { get; set; } 
}

[XmlRoot(ElementName="cert")]
public class Cert { 

    [XmlAttribute(AttributeName="href")] 
    public string Href { get; set; } 
}

[XmlRoot(ElementName="key")]
public class Key { 

    [XmlAttribute(AttributeName="href")] 
    public string Href { get; set; } 
}

[XmlRoot(ElementName="tls")]
public class Tls { 

    [XmlElement(ElementName="cert")] 
    public Cert Cert { get; set; } 

    [XmlElement(ElementName="key")] 
    public Key Key { get; set; } 

    [XmlAttribute(AttributeName="enabled")] 
    public bool Enabled { get; set; } 

    [XmlAttribute(AttributeName="port")] 
    public int Port { get; set; } 
}

[XmlRoot(ElementName="route")]
public class Route { 

    [XmlAttribute(AttributeName="id")] 
    public string Id { get; set; } 

    [XmlAttribute(AttributeName="value")] 
    public string Value { get; set; } 
}

[XmlRoot(ElementName="maps")]
public class Maps { 

    [XmlElement(ElementName="route")] 
    public List<Route> Route { get; set; } 
}

[XmlRoot(ElementName="configuration")]
public class Configuration { 

    [XmlElement(ElementName="static")] 
    public Static Static { get; set; } 

    [XmlElement(ElementName="tls")] 
    public Tls Tls { get; set; } 

    [XmlElement(ElementName="maps")] 
    public Maps Maps { get; set; }


    public static Configuration? Load()
    {
        XmlSerializer serializer = new(typeof(Configuration));
        using var stream = File.Open("configuration.xml", FileMode.Open);

        return serializer.Deserialize(stream) as Configuration;
    }
}

