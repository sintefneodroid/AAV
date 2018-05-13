using System.Collections.Generic;
using UnityEngine;

namespace Neodroid.Utilities.SerialisableDictionary {
  /// <inheritdoc />
  ///  <summary>
  ///  </summary>
  [CreateAssetMenu(menuName = "Example Asset")]
  public class Example : ScriptableObject {
    /// <summary>
    /// 
    /// </summary>
    [SerializeField]
    GameObjectFloatDictionary _game_object_float_store =
        GameObjectFloatDictionary.New<GameObjectFloatDictionary>();

    /// <summary>
    ///
    /// </summary>
    [SerializeField]
    StringIntDictionary _string_integer_store = StringIntDictionary.New<StringIntDictionary>();

    /// <summary>
    ///
    /// </summary>
    Dictionary<string, int> StringIntegers { get { return this._string_integer_store._Dict; } }

    /// <summary>
    ///
    /// </summary>
    Dictionary<GameObject, float> Screenshots { get { return this._game_object_float_store._Dict; } }
  }
}
