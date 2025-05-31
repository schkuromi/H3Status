// TEMPLATE OVERLAY
// ----------------------------------------------
// This example demonstrates how to create a
// basic overlay. The data provided by H3Status
// can be used anywhere that supports WebSockets,
// but JavaScript gives us an easy way to build
// an interface we can use in software like OBS.
//
// The Protocol.md file in this repository
// contains a detailed description of all
// events that can be sent by the server.
// ----------------------------------------------

// Let's get the text elements from the HTML
// so that we can change their values later.
const score = document.getElementById("score");
const health = document.getElementById("health");
const ammo = document.getElementById("ammo");

// This is the port that H3Status uses by default.
// If a user changes the port used by H3Status,
// it must be changed here as well.
const port = 9504;

// Create a new connection to H3Status.
// We have to attach some functions below.
const ws = new WebSocket("ws://localhost:" + port);

// This function runs if we successfully
// connect to the WebSocket server.
ws.onopen = function () {
  console.log("Server connected");
};

// This function runs if we lose connection,
// or if we fail to connect to the server.
ws.onclose = function () {
  console.log("Server disconnected");
};

// This function runs whenever we get
// a new message from the server.
ws.onmessage = function (msg) {
  // Messages are sent in the JSON format,
  // so let's convert them to usable objects.
  const event = JSON.parse(msg.data);

  // Every event has a "type" value that
  // tells us what just happened in the game.
  // Using this, we can decide what we want
  // to change in our overlay.

  // Many events also have a "status" value that
  // contains more information about the event.

  switch (event.type) {
    // We recieve this event every time
    // the Take and Hold score changes
    case "TNHScoreEvent":
      setScore(event.status);
      break;
    // These events both contain the
    // same status information
    case "playerDamage":
    case "playerHeal":
      setHealth(event.status);
      break;
    // We recieve this event every time
    // a weapon's ammunition changes
    case "ammoEvent":
      setAmmo(event.status);
      break;
    // When the scene changes, we should
    // reset our overlay back to default
    case "sceneEvent":
      score.textContent = "0";
      health.textContent = "0/0";
      ammo.textContent = "0/0";
      break;
  }
};

// When we're handling multiple events,
// it is useful to create functions
// for each one. These function change
// each text element on our overlay.

function setScore(status) {
  score.textContent = status.score;
}

function setHealth(status) {
  health.textContent = status.health + "/" + status.maxHealth;
}

function setAmmo(status) {
  ammo.textContent = status.current + "/" + status.capacity;
}

// This is all we need to create a functional overlay!
// To see all events available, check out the
// Protocol.md file on the repository homepage.
