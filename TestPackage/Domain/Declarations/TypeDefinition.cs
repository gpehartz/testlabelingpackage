namespace ICETeam.TestPackage.Domain.Declarations
{
    public class TypeDefinition
    {
        public string NameSpace { get; set; }
        public string Type { get; set; }

        public string GetFullName => NameSpace + "." + Type;
    }
}