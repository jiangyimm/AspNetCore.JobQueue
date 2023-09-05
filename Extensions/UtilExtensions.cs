using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace AspNetCore.JobQueue.Extensions;

internal static class UtilExtensions
{
    internal static string ToHash(this string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        var sb = new StringBuilder();

        for (var i = 0; i < hashBytes.Length; i++)
            sb.Append(hashBytes[i].ToString("x2"));

        return sb.ToString();
    }

    public static IEnumerable<Assembly> GetAssemblies()
    {
        var assemblys = AppDomain.CurrentDomain.GetAssemblies();
        return assemblys;
    }

    public static IEnumerable<TypeInfo> GetTypesAssignableTo(Type targetType)
    {
        var typeInfos = new List<TypeInfo>();
        var assemblys = GetAssemblies();
        foreach (var assembly in assemblys)
        {
            var assTypes = assembly.DefinedTypes.Where(x => x.IsClass
                                && !x.IsAbstract
                                && x != targetType
                                && x.GetInterfaces()
                                        .Any(i => i.IsGenericType
                                                && i.GetGenericTypeDefinition() == targetType))?.ToList();
            typeInfos.AddRange(assTypes);
        }
        return typeInfos;
    }

}
