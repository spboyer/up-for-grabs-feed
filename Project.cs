using System.Collections.Generic;

namespace up_for_grabs_feed
{
  partial class Program
  {
    public class Project
    {
      public string name { get; set; }
      public string desc { get; set; }
      public string site { get; set; }
      public List<string> tags { get; set; }
      public Tags upforgrabs { get; set; }
    }
  }
}
