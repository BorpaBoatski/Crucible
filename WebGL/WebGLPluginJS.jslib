// Creating functions for the Unity
mergeInto(LibraryManager.library, {

   // Method used to send a message to the page
   GAME_LOADED: function () {
      GAME_LOADED();
   },

   GAME_STOPPED_ERROR: function (_Error) {
      GAME_STOPPED_ERROR(Pointer_stringify(_Error));
   },

   GAME_STOPPED: function (_Results) {
      GAME_STOPPED(Pointer_stringify(_Results));
   },

   GAME_CLOSE: function()
   {
      GAME_CLOSE();
   }
});