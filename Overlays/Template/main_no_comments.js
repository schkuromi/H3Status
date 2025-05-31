// TEMPLATE OVERLAY

const score = document.getElementById("score");
const health = document.getElementById("health");
const ammo = document.getElementById("ammo");

const port = 9504;
const ws = new WebSocket("ws://localhost:" + port);

ws.onopen = function () {
  console.log("Server connected");
};

ws.onclose = function () {
  console.log("Server disconnected");
};

ws.onmessage = function (msg) {
  const event = JSON.parse(msg.data);

  switch (event.type) {
    case "TNHScoreEvent":
      setScore(event.status);
      break;
    case "playerDamage":
    case "playerHeal":
      setHealth(event.status);
      break;
    case "ammoEvent":
      setAmmo(event.status);
      break;
    case "sceneEvent":
      score.textContent = "0";
      health.textContent = "0/0";
      ammo.textContent = "0/0";
      break;
  }
};

function setScore(status) {
  score.textContent = status.score;
}

function setHealth(status) {
  health.textContent = status.health + "/" + status.maxHealth;
}

function setAmmo(status) {
  ammo.textContent = status.current + "/" + status.capacity;
}
