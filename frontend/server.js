import express from "express";

const app = express();
const port = process.env.PORT || 3000;

app.get("/", (_req, res) => {
  res.status(200).json({ service: "ModernizationPlatform.Frontend", status: "ok" });
});

app.listen(port, () => {
  console.log(`Frontend listening on port ${port}`);
});
