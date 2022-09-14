namespace Lab2.services
{
    public struct Argument
    {
        public string Name { get; }
        public string Type { get; }
        public ArgumentType ArgumentType { get;  }

        public Argument(AccessModifier accessModifier, string type, string name, ArgumentType argumentType)
        {
            Name = name;
            Type = type;
            ArgumentType = argumentType;
        }
    }
}