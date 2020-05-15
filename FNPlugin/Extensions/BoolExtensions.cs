using JetBrains.Annotations;

namespace FNPlugin.Extensions
{
    public static class BooleanExtensions
    {
        public static int ToInt(this bool value)
        {
            return value ? 1 : 0;
        }

        public static bool IsNullOr(this bool? value, bool testValue)
        {
            if (value == null) return true;

            return value == testValue;
        }

        [ContractAnnotation("value:false => true")]
        public static bool IsFalse(this bool value)
        {
            return value == false;
        }
    }
}
