using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZkData
{
    public partial class SpringBattle
    {

        public SpringBattle()
        {
            AccountBattleAwards = new HashSet<AccountBattleAward>();
            SpringBattlePlayers = new HashSet<SpringBattlePlayer>();
            Events = new HashSet<Event>();
        }

        public int SpringBattleID { get; set; }

        [StringLength(64)]
        public string EngineGameID { get; set; }

        public int HostAccountID { get; set; }

        [StringLength(200)]
        public string Title { get; set; }

        public int MapResourceID { get; set; }

        public int ModResourceID { get; set; }

        public DateTime StartTime { get; set; }

        public int Duration { get; set; }

        public int PlayerCount { get; set; }

        public bool HasBots { get; set; }

        public bool IsMission { get; set; }

        [StringLength(500)]
        public string ReplayFileName { get; set; }

        [StringLength(100)]
        public string EngineVersion { get; set; }

        public bool IsEloProcessed { get; set; }

        public int? WinnerTeamXpChange { get; set; }

        public int? LoserTeamXpChange { get; set; }

        public int? ForumThreadID { get; set; }

        [StringLength(250)]
        public string TeamsTitle { get; set; }

        public bool IsFfa { get; set; }

        public int? RatingPollID { get; set; }

        public virtual Account Account { get; set; }

        
        public virtual ICollection<AccountBattleAward> AccountBattleAwards { get; set; }

        public virtual ForumThread ForumThread { get; set; }

        public virtual Resource ResourceByModResourceID { get; set; }

        public virtual Resource ResourceByMapResourceID { get; set; }
        
        public virtual ICollection<SpringBattlePlayer> SpringBattlePlayers { get; set; }

        
        public virtual ICollection<Event> Events { get; set; }
    }
}