
public static class SerializeOpMgr
{
    private static int _g_serializeOp = 1;

    public static int next()
    {
        _g_serializeOp++;
        return _g_serializeOp;
    }
}