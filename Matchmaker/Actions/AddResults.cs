﻿using Matchmaker.Logic;
using Moserware.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Matchmaker.Actions
{
    public class AddResults : Action
    {
        public override string Name => "add results";
        public override string Description => "asks for results and updates scores for both teams";
        public override string Usage => "add results <winner: blue/red>";

        public override bool Call(string[] arguments)
        {
            if (arguments.Length != 1)
            {
                return false;
            }

            TeamColor? color = TeamColorExtensions.FromString(arguments[0]);

            if (!color.HasValue)
            {
                Console.WriteLine("Color not found");
                return false;
            }

            var redTeam = new Moserware.Skills.Team();
            var blueTeam = new Moserware.Skills.Team();

            using (Context context = new Context())
            {
                foreach (int i in Matchmaker.Logic.Teams.Red.Participants)
                {
                    Participant participant = context.Participants.Find(i);
                    redTeam.AddPlayer(
                        new Player(participant.ParticipantId),
                        new Rating(
                            participant.Mean,
                            participant.StandardDeviation));
                }

                foreach (int i in Matchmaker.Logic.Teams.Blue.Participants)
                {
                    Participant participant = context.Participants.Find(i);
                    blueTeam.AddPlayer(
                        new Player(participant.ParticipantId),
                        new Rating(
                            participant.Mean,
                            participant.StandardDeviation));
                }
            }

            IDictionary<Player, Rating> newRating = TrueSkillCalculator.CalculateNewRatings(
                GameInfo.DefaultGameInfo,
                Moserware.Skills.Teams.Concat(
                    redTeam,
                    blueTeam),
                color.Value == TeamColor.Blue ? 2 : 1,
                color.Value == TeamColor.Blue ? 1 : 2);

            using (Context context = new Context())
            {
                foreach (KeyValuePair<Player, Rating> player in newRating)
                {
                    Participant participant = context.Participants.Find(player.Key.Id);

                    if (participant == null)
                    {
                        Console.WriteLine($"Failed to find player {player.Key.Id.ToString()}");

                        continue;
                    }

                    Console.WriteLine($"Player {participant.Name}: rating({player.Value.ConservativeRating}) Old [mean({participant.Mean}) sd({participant.StandardDeviation})] New [mean({player.Value.Mean}) sd({player.Value.StandardDeviation})]");

                    participant.Mean = player.Value.Mean;
                    participant.StandardDeviation = player.Value.StandardDeviation;
                    participant.Rating = player.Value.ConservativeRating;
                }

                context.SaveChanges();
            }


            return true;
        }
    }
}
