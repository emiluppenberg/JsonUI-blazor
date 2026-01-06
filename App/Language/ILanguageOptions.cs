public interface ILanguageOptions
{
  string GetClass(JNodeClass jnc);
  Array GetCollectionOptions();
  string ConfigureCollection(string datatype, string collectionAs);
}