﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Transactions;
using System.Web.Services;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb
{
  /// <summary>
  /// Summary description for ContentService
  /// </summary>
  [WebService(Namespace = "http://tempuri.org/")]
  [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
  [ToolboxItem(false)]
  // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
  public class ContentService: WebService
  {
    [WebMethod]
    public bool DownloadFile(string internalName,
                             out List<string> links,
                             out byte[] torrent,
                             out List<string> dependencies,
                             out ZkData.ResourceType resourceType,
                             out string torrentFileName)
    {
      return PlasmaServer.DownloadFile(internalName, out links, out torrent, out dependencies, out resourceType, out torrentFileName);
    }

    /// <summary>
    /// Finds resource by either md5 or internal name
    /// </summary>
    /// <param name="md5"></param>
    /// <param name="internalName"></param>
    /// <returns></returns>
    [WebMethod]
    public PlasmaServer.ResourceData GetResourceData(string md5, string internalName)
    {
      return PlasmaServer.GetResourceData(md5, internalName);
    }



    [WebMethod]
    public bool SubmitSpringBattleResult(string accountName, string password, string engineBattleID, string engineVersion, string mod, string map, bool isMission, bool isBots, string replayName, DateTime startTime, int duration, string title, List<BattlePlayerResult> players)
    {
      var acc = AuthServiceClient.VerifyAccountPlain(accountName, password);
      if (acc == null) throw new Exception("Account name or password not valid");

      var db = new ZkDataContext();
      var sb = new SpringBattle()
               {
                 HostAccountID = acc.AccountID,
                 Duration = duration,
                 EngineGameID = engineBattleID,
                 MapResourceID = db.Resources.Single(x => x.InternalName == map).ResourceID,
                 ModResourceID = db.Resources.Single(x => x.InternalName == mod).ResourceID,
                 HasBots = isBots,
                 IsMission = isMission,
                 PlayerCount = players.Count(x => !x.IsSpectator),
                 StartTime = startTime,
                 Title = title,
                 ReplayFileName = replayName,
                 EngineVersion = engineVersion,
               };
      db.SpringBattles.InsertOnSubmit(sb);

      foreach (var p in players)
      {
        sb.SpringBattlePlayers.Add(new SpringBattlePlayer()
                                   {
                                     AccountID = p.AccountID,
                                     AllyNumber = p.AllyNumber,
                                     CommanderType = p.CommanderType,
                                     IsInVictoryTeam = p.IsVictoryTeam,
                                     IsSpectator = p.IsSpectator,
                                     Rank = p.Rank,
                                     LoseTime = p.LoseTime
                                   });
      }
      
      db.SubmitChanges();
      foreach (var p in players)
      {
        foreach (var a in p.Awards)
        {
          db.AccountBattleAwards.InsertOnSubmit(new AccountBattleAward()
                                                {
                                                  AccountID = p.AccountID,
                                                  SpringBattleID = sb.SpringBattleID,
                                                  AwardKey = a.Award,
                                                  AwardDescription = a.Description
                                                });

          
        }

        foreach (var s in p.Stats)
        {
          db.AccountBattleStats.InsertOnSubmit(new AccountBattleStat()
                                               {
                                                 AccountID = p.AccountID,
                                                 SpringBattleID = sb.SpringBattleID,
                                                 StatsKey = s.Key,
                                                 Value = s.Value
                                               });

        }
      }

      db.SubmitChanges();

      return true;  
    }


    [WebMethod]
    public List<PlasmaServer.ResourceData> GetResourceList(DateTime? lastChange, out DateTime currentTime)
    {
      return PlasmaServer.GetResourceList(lastChange, out currentTime);
    }


    [WebMethod]
    public ScriptMissionData GetScriptMissionData(string name)
    {
      using (var db = new ZkDataContext())
      {
        var m = db.Missions.Single(x => x.Name == name && x.IsScriptMission);
        return new ScriptMissionData()
               {
                 MapName = m.Map,
                 ModTag = m.ModRapidTag,
                 StartScript = m.Script,
                 ManualDependencies = m.ManualDependencies != null ? new List<string>(m.ManualDependencies.Split('\n')) : null,
                 Name = m.Name
               };
      }
    }


    [WebMethod]
    public void NotifyMissionRun(string login, string missionName)
    {
      using (var db = new ZkDataContext())
      using (var scope = new TransactionScope())
      {
        db.Missions.Single(x => x.Name == missionName).MissionRunCount++;
        db.Accounts.Single(x => x.Name == login).MissionRunCount++;
        db.SubmitChanges();
        scope.Complete();
      }
    }


    [WebMethod]
    public PlasmaServer.ReturnValue RegisterResource(int apiVersion,
                                                     string springVersion,
                                                     string md5,
                                                     int length,
                                                     ZkData.ResourceType resourceType,
                                                     string archiveName,
                                                     string internalName,
                                                     int springHash,
                                                     byte[] serializedData,
                                                     List<string> dependencies,
                                                     byte[] minimap,
                                                     byte[] metalMap,
                                                     byte[] heightMap,
                                                     byte[] torrentData)
    {
      return PlasmaServer.RegisterResource(apiVersion,
                                           springVersion,
                                           md5,
                                           length,
                                           resourceType,
                                           archiveName,
                                           internalName,
                                           springHash,
                                           serializedData,
                                           dependencies,
                                           minimap,
                                           metalMap,
                                           heightMap,
                                           torrentData);
    }

    [WebMethod]
    public void SubmitMissionScore(string login, string passwordHash, string missionName, int score, int gameSeconds)
    {
      using (var db = new ZkDataContext())
      {
        var acc = AuthServiceClient.VerifyAccountHashed(login, passwordHash);
        if (acc == null) throw new ApplicationException("Invalid login or password");

        var mission = db.Missions.Single(x => x.Name == missionName);

        var scoreEntry = mission.MissionScores.FirstOrDefault(x => x.AccountID == acc.AccountID);
        if (scoreEntry == null)
        {
          scoreEntry = new MissionScore() { MissionID = mission.MissionID, AccountID = acc.AccountID, Score = int.MinValue };
          mission.MissionScores.Add(scoreEntry);
        }

        if (score > scoreEntry.Score)
        {
          var max = mission.MissionScores.Max(x => (int?)x.Score);
          if (max == null || max <= score) mission.TopScoreLine = login;
          scoreEntry.Score = score;
          scoreEntry.Time = DateTime.UtcNow;
          scoreEntry.MissionRevision = mission.Revision;
          scoreEntry.GameSeconds = gameSeconds;
          db.SubmitChanges();
        }
      }
    }

    [WebMethod]
    public void SubmitStackTrace(ProgramType programType, string playerName, string exception, string extraData, string programVersion)
    {
      using (var db = new ZkDataContext())
      {
        var exceptionLog = new ExceptionLog
                           {
                             ProgramID = programType,
                             Time = DateTime.UtcNow,
                             PlayerName = playerName,
                             ExtraData = extraData,
                             Exception = exception,
                             ExceptionHash = new Hash(exception).ToString(),
                             ProgramVersion = programVersion,
                             RemoteIP = GetUserIP()
                           };
        db.ExceptionLogs.InsertOnSubmit(exceptionLog);
        db.SubmitChanges();
      }
    }


    string GetUserIP()
    {
      var ip = Context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
      if (string.IsNullOrEmpty(ip) || ip.Equals("unknown", StringComparison.OrdinalIgnoreCase)) ip = Context.Request.ServerVariables["REMOTE_ADDR"];
      return ip;
    }

    public class ScriptMissionData
    {
      public List<string> ManualDependencies;
      public string MapName;
      public string ModTag;
      public string Name;
      public string StartScript;
    }

    public class BattlePlayerResult
    {
      public int AccountID;
      public bool IsSpectator;
      public bool IsVictoryTeam;
      public string CommanderType;
      public int? LoseTime;
      public int AllyNumber;
      public int Rank;
      public List<PlayerAward> Awards;
      public List<PlayerStats> Stats;

      public class PlayerStats
      {
        public string Key;
        public double Value;
      }

      public class PlayerAward
      {
        public string Award;
        public string Description;
      }
    }

  }


}