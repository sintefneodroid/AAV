﻿using FlatBuffers;

namespace Neodroid.Utilities.Messaging.FBS {
  public static class CustomFlatBufferImplementation {
    //Custom implementation of copying bytearray, faster than generated code
    public static VectorOffset CreateByteVector(FlatBufferBuilder builder, byte[] data) {
      //builder.StartVector(1, data.Length, 1);
      //var additional_bytes = data.Length - 2;
      //builder.Prep(sizeof(byte), additional_bytes * sizeof(byte));

      // for (var i = data.Length - 1; i >= 0; i--)
      //  builder.PutByte(data[i]);
      //return builder.EndVector();

      return builder.CreateByteVector(data);
    }

    public static VectorOffset CreateFloatVector(FlatBufferBuilder builder, float[] data) {
      //builder.StartVector(1, data.Length, 1);
      //var additional_bytes = data.Length - 2;
      //builder.Prep(sizeof(byte), additional_bytes * sizeof(byte));

      // for (var i = data.Length - 1; i >= 0; i--)
      //  builder.PutByte(data[i]);
      //return builder.EndVector();

      return builder.CreateFloatVector(data);
    }
  }
}
