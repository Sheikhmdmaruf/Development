/* ======================================================
   CASE STUDIES — shown on project.html?p=<slug>
   The key is the project's slug (title in lowercase with dashes),
   e.g. "Food Delivery App" -> "food-delivery-app".

   Fields:
     client, role, duration, tools, team  -> details panel (top right)
     metrics: 3 cards under the header
     steps:   numbered story panels — each has a title and paragraphs
     gallery: images at the end — 1 wide + 2 half
              (use paths like "images/food-1.jpg"; leave out to show placeholders)

   Projects without an entry still get a page with an overview.

   !! The Food Delivery App entry below is SAMPLE text with made-up
   !! numbers. Replace it with your real story before publishing.
   ====================================================== */
const CASES = {
  "food-delivery-app": {
    client: "Food delivery startup",
    role: "Product designer (UI/UX)",
    duration: "6 weeks",
    tools: "Figma, FigJam, Maze",
    team: "1 designer, 1 PM, 2 developers",
    metrics: [
      {label:"Checkout", value:"-35%", cap:"fewer steps to place an order", icon:"cart"},
      {label:"Timeline", value:"6 weeks", cap:"from research to handoff", icon:"clock"},
      {label:"Screens", value:"40+", cap:"designed and prototyped", icon:"grid"}
    ],
    steps: [
      {title:"Challenge", body:[
        "A growing food delivery service with 20K+ monthly users wanted its app to feel faster and friendlier, but the path to checkout was long and confusing.",
        "Many carts were left before payment, search results were hard to scan and delivery fees appeared only at the last step."]},
      {title:"Process", body:[
        "I ran 12 user interviews, audited 5 competitor apps and reviewed 200+ support tickets, then set one goal: get from hunger to order in under a minute.",
        "Wireframes and a clickable prototype were tested with 15 people over 3 rounds. The final UI uses a warm palette, large food photography and a reusable Figma component library."]},
      {title:"Result", body:[
        "Checkout now takes 35% fewer steps, a past order can be repeated in two taps and the team has 40+ reusable components to build new features faster.",
        "Next steps: personalised recommendations, group ordering and dark mode."]}
    ]
    // gallery: ["images/food-1.jpg", "images/food-2.jpg", "images/food-3.jpg"]
  }
};
