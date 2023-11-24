namespace NobunAtelier
{
    public class AttributesModule : CharacterAbilityModuleBase
    {
    }

    public class AttributeBase<T>
    {
        public delegate void OnAttributeValueChangeDelegate(T value);

        // public event OnAttributeValueChangeDelegate OnValueChangeEvent;
    }
}