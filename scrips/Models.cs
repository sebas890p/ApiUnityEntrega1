using System;

// Respuesta de la API falsa (my-json-server / db.json)
// GET https://my-json-server.typicode.com/<usuario>/<repo>/users/<id>
[Serializable]
public class UserInfo
{
    public int id;
    public string username;
    public int[] deck; // IDs de las "cartas" (personajes) del usuario
}

// Respuesta de la API de terceros (Rick and Morty API)
// GET https://rickandmortyapi.com/api/character/<id>
[Serializable]
public class Character
{
    public int id;
    public string name;
    public string species;
    public string image;
}

// Wrapper para poder parsear un ARRAY de personajes con JsonUtility.
// JsonUtility no puede parsear un JSON que empieza directo con "[",
// por eso lo envolvemos como {"characters":[...]} antes de parsear.
// Se usa cuando pedimos varios IDs en una sola llamada:
// GET https://rickandmortyapi.com/api/character/1,2,3,4,5
[Serializable]
public class CharacterListWrapper
{
    public Character[] characters;
}