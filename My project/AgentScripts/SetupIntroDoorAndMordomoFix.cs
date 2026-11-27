using ProjectRac.Editor.Map;

public static class SetupIntroDoorAndMordomoFix
{
    public static object Apply()
    {
        var director = IntroDoorSequenceBuilder.SetupInScene();
        return new
        {
            ok = director != null,
            directorName = director != null ? director.gameObject.name : "null",
            pos = director != null ? director.transform.position.ToString("F2") : "null"
        };
    }
}
