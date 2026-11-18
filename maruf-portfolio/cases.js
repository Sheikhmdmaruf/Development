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
      {title:"Overview", body:[
        "A growing food delivery service wanted its app to feel faster and friendlier. It serves 20K+ monthly users and 300+ partner restaurants across 5 cities.",
        "My job was to redesign the full ordering experience, from finding food to tracking the rider, and to build a simple design system the team could reuse."]},
      {title:"Problem", body:[
        "Users opened the app hungry and in a hurry, but the path to checkout was long and confusing. Many carts were left before payment, search results were hard to scan and delivery fees appeared only at the last step."]},
      {title:"Research & discovery", body:[
        "I ran 12 user interviews, audited 5 competitor apps and reviewed 200+ support tickets to find where people got stuck.",
        "Key findings: people decide by photo and delivery time, hidden fees break trust, and repeat orders take as long as new ones."]},
      {title:"Ideation & user flow", body:[
        "I sketched several directions and chose one goal: get from hunger to order in under a minute.",
        "I mapped three core journeys — discover, reorder and track — and removed every step that didn't help the user decide or pay."]},
      {title:"Wireframing & visual design", body:[
        "Low-fidelity wireframes let us test layout and flow early. The final UI uses a warm, appetising palette, large food photography and clear type, built on a reusable Figma component library."]},
      {title:"Testing & iterations", body:[
        "I tested a clickable prototype with 15 people over 3 rounds, reaching 90% task success.",
        "Testing showed users missed the reorder button and wanted fees earlier, so I moved reorder to the home screen and showed the total price on every restaurant card."]},
      {title:"Result", body:[
        "Checkout now takes 35% fewer steps, a past order can be repeated in two taps and the team has 40+ reusable components to build new features faster.",
        "Next steps: personalised recommendations, group ordering and dark mode."]}
    ]
    // gallery: ["images/food-1.jpg", "images/food-2.jpg", "images/food-3.jpg"]
  }
};
