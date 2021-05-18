using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPS.Web
{
    public class PlayerTupleConverter : JsonConverter<(Player PlayerOne, Player PlayerTwo)>
    {
        public override (Player PlayerOne, Player PlayerTwo) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            (Player PlayerOne, Player PlayerTwo) players = new();
            var startDepth = reader.CurrentDepth;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == startDepth)
                    break;
                if (reader.TokenType == JsonTokenType.PropertyName)
                    if (reader.GetString() == nameof(GameState.Players.PlayerOne))
                    {
                        players.PlayerOne = Read(reader);
                    }
                    else if (reader.GetString() == nameof(GameState.Players.PlayerTwo))
                    {
                        players.PlayerTwo = Read(reader);
                    }
            }
            return players;
        }

        public static Player Read(Utf8JsonReader reader)
        {
            var p = new Player(default, default);
            var propertyCount = 0;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == nameof(Player.Id))
                {
                    reader.Read();
                    p = p with { Id = reader.GetString() };
                    propertyCount++;
                }
                if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == nameof(Player.Hand))
                {
                    reader.Read();
                    p = p with { Hand = (Hand)reader.GetInt16() };
                    propertyCount++;
                }
                if (propertyCount == 2)
                    break;
            }
            return p;
        }

        public override void Write(Utf8JsonWriter writer, (Player PlayerOne, Player PlayerTwo) value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteStartObject(nameof(value.PlayerOne));
            writer.WriteString(nameof(Player.Id), value.PlayerOne.Id);
            writer.WriteNumber(nameof(Player.Hand), (int)value.PlayerOne.Hand);
            writer.WriteEndObject();
            writer.WriteStartObject(nameof(value.PlayerTwo));
            writer.WriteString(nameof(Player.Id), value.PlayerTwo.Id);
            writer.WriteNumber(nameof(Player.Hand), (int)value.PlayerTwo.Hand);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
    }
}
