using System;
using System.Collections.Generic;
using System.Linq;
using ai_csp.Dtos;
namespace ai_csp.Services
{
    public class SchedulerService
    {
        private static List<string> TEAMS = new List<string>
        {
            "Al Ahly", "Zamalek", "Pyramids", "Al Masry",
            "Ceramica Cleopatra", "Al Bank Al Ahly", "Al Ittihad", "ZED",
            "Modern Sport", "Wadi Degla", "Petrojet", "ENPPI",
            "Ismaily", "Al Mokawloon", "Smouha", "Tala'ea El Gaish",
            "Ghazl El Mahalla", "Farco"
        };

        private static Dictionary<string, string> STADIUMS = new Dictionary<string, string>
        {
            {"Al Ahly","Cairo International Stadium"},
            {"Zamalek","Cairo International Stadium"},
            {"Pyramids","30 June Stadium"},
            {"Al Masry","Suez Stadium"},
            {"Ceramica Cleopatra","Osman Ahmed Osman Stadium"},
            {"Al Bank Al Ahly","Military College Stadium"},
            {"Al Ittihad","Alexandria Stadium"},
            {"ZED","Canal Suez Stadium"},
            {"Modern Sport","Salam Stadium"},
            {"Wadi Degla","Petro Sport Stadium"},
            {"Petrojet","Military Academy Stadium"},
            {"ENPPI","Petro Sport Stadium"},
            {"Ismaily","Ismailia Stadium"},
            {"Al Mokawloon","Osman Ahmed Osman Stadium"},
            {"Smouha","Borg El Arab Stadium"},
            {"Tala'ea El Gaish","Gehaz El Reyada Stadium"},
            {"Ghazl El Mahalla","Ghazl El Mahalla Stadium"},
            {"Farco","Haras El Hodoud Stadium"},
        };

        private static DateTime SEASON_START = new DateTime(2025, 8, 8);
        private static DateTime SEASON_END = new DateTime(2026, 6, 7);

        private static HashSet<int> PREFERRED_DAYS = new HashSet<int> { 3, 4, 5 };

        private static List<string> DAY_TIMES = new List<string> { "16:00", "20:00", "22:00" };
        private static List<string> EXTRA_TIMES = new List<string> { "14:00", "18:00" };

       
        private Dictionary<string, DateTime> team_last_match = new();
        private Dictionary<string, int> team_home_streak = new();
        private Dictionary<(string, DateTime), HashSet<string>> stadium_day_times = new();

        private List<DateTime> GetWindow(int roundIdx, int size = 5)
        {
            var baseDate = SEASON_START.AddDays(roundIdx * 7);

            for (int offset = 0; offset < 28; offset++)
            {
                var start = baseDate.AddDays(offset);
                if (start < SEASON_START || start > SEASON_END.AddDays(-2))
                    continue;

                var window = new List<DateTime>();
                for (int d = 0; d < size; d++)
                {
                    var day = start.AddDays(d);
                    if (day >= SEASON_START && day <= SEASON_END)
                        window.Add(day);
                }

                if (window.Count >= 3)
                    return window;
            }

            return Enumerable.Range(0, size)
                .Select(i => SEASON_START.AddDays(roundIdx * 7 + i))
                .ToList();
        }

        private bool Check72h(string team, DateTime newDate, List<(string, string, DateTime)> temp)
        {
            if (team_last_match.ContainsKey(team))
            {
                var last = team_last_match[team];
                if (Math.Abs((newDate - last).TotalDays) < 3)
                    return false;
            }

            foreach (var m in temp)
            {
                if (m.Item1 == team || m.Item2 == team)
                {
                    if (Math.Abs((newDate - m.Item3).TotalDays) < 3)
                        return false;
                }
            }

            return true;
        }


        private (List<List<(int, int)>>, List<List<(int, int)>>) MakeRounds()
        {
            int n = TEAMS.Count;
            int half = n / 2;

            var rotation = Enumerable.Range(0, n).ToList();
            var first_leg = new List<List<(int, int)>>();

            for (int r = 0; r < n - 1; r++)
            {
                var round = new List<(int, int)>();

                for (int i = 0; i < half; i++)
                    round.Add((rotation[i], rotation[n - 1 - i]));

                first_leg.Add(round);

                rotation = new List<int> { rotation[0], rotation[^1] }
                    .Concat(rotation.Skip(1).Take(n - 2)).ToList();
            }

            var second_leg = first_leg
                .Select(r => r.Select(p => (p.Item2, p.Item1)).ToList())
                .ToList();

            return (first_leg, second_leg);
        }

        private List<List<(int, int)>> Interleave(List<List<(int, int)>> first_leg, List<List<(int, int)>> second_leg)
        {
            var result = new List<List<(int, int)>>();

            for (int i = 0; i < first_leg.Count; i++)
            {
                result.Add(first_leg[i]);
                result.Add(second_leg[i]);
            }

            return result;
        }

        private (string, string, DateTime, string, string)? AssignMatch(
            string home,
            string away,
            List<DateTime> days,
            Dictionary<DateTime, List<string>> used,
            List<(string, string, DateTime)> temp)
        {
            var sortedDays = days
                .OrderBy(d => used[d].Count)
                .ThenBy(d => PREFERRED_DAYS.Contains((int)d.DayOfWeek) ? 0 : 1)
                .ThenBy(d => d)
                .ToList();

            foreach (var day in sortedDays)
            {
                if (used[day].Count >= 3)
                    continue;

                if (!Check72h(home, day, temp) || !Check72h(away, day, temp))
                    continue;

                var freeTimes = DAY_TIMES.Where(t => !used[day].Contains(t)).ToList();

                if (!freeTimes.Any())
                    freeTimes = EXTRA_TIMES.Where(t => !used[day].Contains(t)).ToList();

                if (!freeTimes.Any())
                    continue;

                var stadium = STADIUMS[home];

                foreach (var time in freeTimes)
                {
                    var key = (stadium, day);

                    if (stadium_day_times.ContainsKey(key) &&
                        stadium_day_times[key].Contains(time))
                        continue;

                    return (home, away, day, time, stadium);
                }
            }

            return null;
        }

        private List<MatchesDto> Schedule()
        {
            var (first_leg, second_leg) = MakeRounds();
            var rounds = Interleave(first_leg, second_leg);

            var solution = new List<MatchesDto>();

            foreach (var team in TEAMS)
                team_home_streak[team] = 0;

            for (int r = 0; r < rounds.Count; r++)
            {
                var pairs = rounds[r]
                    .Select(p => (TEAMS[p.Item1], TEAMS[p.Item2]))
                    .ToList();

                var window = GetWindow(r);
                var used = window.ToDictionary(d => d, d => new List<string>());
                var temp = new List<(string, string, DateTime)>();

                foreach (var (home, away) in pairs)
                {
                    var res = AssignMatch(home, away, window, used, temp);

                    if (res != null)
                    {
                        var (h, a, d, t, s) = res.Value;

                        temp.Add((h, a, d));
                        used[d].Add(t);

                        solution.Add( new MatchesDto
                        {
                            Round = r + 1,
                            Date = d.ToString("yyyy-MM-dd"),
                            Time = t,
                            Home = h,
                            Away = a,
                            Stadium = s
                        });

                        team_last_match[h] = d;
                        team_last_match[a] = d;

                        if (!stadium_day_times.ContainsKey((s, d)))
                            stadium_day_times[(s, d)] = new HashSet<string>();

                        stadium_day_times[(s, d)].Add(t);

                        team_home_streak[h]++;
                        team_home_streak[a] = 0;
                    }
                }
            }

            return solution;
        }

        public List<MatchesDto> GenerateSchedule()
        {
            return Schedule();
        }
    }
}