using System;
public interface IBuildStrategy
{
    void Build(string branch,bool ShouldRun);
}

