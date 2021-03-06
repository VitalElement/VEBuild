﻿namespace VEBuild.Models
{
    using Newtonsoft.Json;
    using System.IO;

    public class SerializedObject<T>
    {
        public void Serialize(string filename)
        {
            var writer = new StreamWriter(filename);
            writer.Write(JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            writer.Close();
        }

        public static T Deserialize (string filename)
        {
            var reader = new StreamReader(filename);

            var result = JsonConvert.DeserializeObject<T>(reader.ReadToEnd());

            reader.Close();

            return result;
        }
    }
}
