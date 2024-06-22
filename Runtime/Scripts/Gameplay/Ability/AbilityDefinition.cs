using NobunAtelier;
using System.Collections.Generic;
using System;

public abstract class AbilityDefinition : DataDefinition
{
    public abstract IAbilityInstance CreateAbilityInstance(AbilityController controller);
}