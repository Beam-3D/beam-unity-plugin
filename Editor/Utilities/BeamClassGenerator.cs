using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Beam.Runtime.Sdk.Generated.Model;
using UnityEngine;

namespace Beam.Editor.Utilities
{
  public class PropertyName
  {
    public PropertyName(string name, string value)
    {
      this.Name = name;
      this.Value = value;

    }
    public string Name;
    public string Value;
  }
  public class BeamClassGenerator
  {
    private static string ProcessPropertyName(string name)
    {
      // Make titlecase
      TextInfo textInfo = new CultureInfo("en-GB", false).TextInfo;
      name = textInfo.ToTitleCase(name);

      // Make alphanumeric
      Regex rgx = new Regex("[^a-zA-Z0-9 -]");
      name = rgx.Replace(name, "");

      // Prefix with _ if starts with a number
      if (!Char.IsLetter(name[0]))
      {
        name = $"_{name}";
      }

      // Replace spaces
      return name.Replace(" ", "_");
    }
    private static string ProcessPropertyValue(string name)
    {
      return name.Replace(@"""", @"\""");
    }
    public static void GenerateUserTagsClass(List<Tag> tags)
    {
      List<string> tagBody = new List<string>();

      TextInfo myTI = new CultureInfo("en-GB", false).TextInfo;

      Regex rgx = new Regex("[^a-zA-Z0-9 -]");
      List<PropertyName> propertyNames = new List<PropertyName>();

      tags.ForEach(ut =>
      {
        string name = ProcessPropertyName(ut.Name);
        int duplicates = propertyNames.Where(p => p.Name == name).Count();
        if (duplicates > 0)
        {
          name = $"{name}{duplicates + 1}";
        }
        propertyNames.Add(new PropertyName(name, ProcessPropertyValue(ut.Name)));
      });


      propertyNames.ForEach(p =>
      {
        tagBody.Add($"    public const string {p.Name} = \"{p.Value}\";");
      });

      // Generate an enum file from tags
      string[] lines = {
            "namespace Beam.Model",
            "{",
            "  public class BeamUserTags",
            "  {",
            string.Join("\r\n", tagBody),
            "  }",
            "}"
        };

      System.IO.File.WriteAllLines($@"{Application.dataPath}/Beam/BeamUserTags.cs", lines);
    }
  }
}
