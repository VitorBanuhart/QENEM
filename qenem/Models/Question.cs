using System.ComponentModel.DataAnnotations.Schema;

namespace qenem.Models
{

    public class Question
    {
        public int id { get; set; } 
        public string title { get; set; }
        public int index { get; set; }
        public int year { get; set; }
        public string? language { get; set; }
        public string discipline { get; set; }
        public string context { get; set; }
        public List<string> files { get; set; }
        public string correctAlternative { get; set; }
        public string alternativesIntroduction { get; set; }
        [NotMapped]
        public List<Alternative> alternatives { get; set; }
        public string UniqueId { get; set; } 
    }

    public class Alternative
    {
        //public int id { get; set; }
        public string letter { get; set; }
        public string text { get; set; }
        public string? file { get; set; }
        public bool isCorrect { get; set; }
    }
}
