mergeInto(LibraryManager.library, {

  SendScoreToUnityroom: function (id, score) {
    if (typeof unityroom !== "undefined") {
      unityroom.SendScore(id, score);
    }
  },

  ShowLeaderboardInternal: function (id) {
    if (typeof unityroom !== "undefined") {
      unityroom.ShowLeaderboard(id);
    }
  }

});
