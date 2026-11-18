/* ======================================================
   CASE STUDIES — shown on project.html?p=<slug>
   The key is the project's slug (title in lowercase with dashes),
   e.g. "Food Delivery App" -> "food-delivery-app".

   draft: true  -> shows a small "Draft case study" note on the page.
   Remove it once the text and numbers are your real ones.

   Projects without an entry here still get a page: header,
   details, cover and a short overview with a Dribbble link.

   Step fields (all optional except title):
     body:   ["paragraph", "paragraph"]
     list:   ["bullet", "bullet"]
     stats:  [["50K+", "active users"], ...]
     quote:  {text:"...", by:"Name, role"}
     beforeAfter: true            (Before / After image pair)
     images: ["full"] or ["half","half"]  (gallery after this step;
             use an image path like "images/food-1.jpg" instead of
             "full"/"half" when you have the picture)
   ====================================================== */
const CASES = {
  "food-delivery-app": {
    draft: true, // SAMPLE CONTENT — replace with your real project story and numbers
    client: "Food delivery startup",
    role: "Product Designer (UI/UX)",
    duration: "6 weeks",
    tools: "Figma, FigJam, Maze",
    team: "1 designer, 1 PM, 2 developers",
    platform: "iOS & Android",
    metrics: [
      {label:"Checkout", value:"-35%", cap:"fewer steps to place an order", icon:"cart"},
      {label:"Timeline", value:"6 weeks", cap:"from research to handoff", icon:"clock"},
      {label:"Screens", value:"40+", cap:"designed and prototyped", icon:"grid"}
    ],
    steps: [
      {title:"Overview",
        body:["A growing food delivery service wanted its app to feel faster and friendlier. Customers loved the restaurants, but ordering took too many taps and the app felt cluttered.",
              "My job was to redesign the full ordering experience — from finding food to tracking the rider — and build a simple design system the team could reuse."],
        stats:[["20K+","monthly users"],["300+","partner restaurants"],["5","cities"]]},
      {title:"Problem",
        body:["Users opened the app hungry and in a hurry, but the path to checkout was long and confusing."],
        list:["Many carts were left before payment","Search results were hard to scan","Delivery fees appeared only at the last step","Order tracking gave little useful information"],
        images:["full"]},
      {title:"Research & discovery",
        body:["I talked with regular customers, reviewed support messages and compared the app with leading competitors to find where people got stuck."],
        stats:[["12","user interviews"],["5","competitor audits"],["200+","support tickets reviewed"]],
        list:["People decide by photo and delivery time, not by long menus","Hidden fees break trust at checkout","Repeat orders are common but take as long as new ones"]},
      {title:"Ideation",
        body:["I sketched several directions and tested the strongest ideas with quick paper prototypes. The chosen direction focused on one goal: get from hunger to order in under a minute."]},
      {title:"User flow",
        body:["I mapped the three most important journeys and removed every step that didn't help the user decide or pay."],
        list:["Discover — browse by craving, distance and delivery time","Reorder — repeat a past order in two taps","Track — see the rider live with clear time estimates"]},
      {title:"Wireframing",
        body:["Low-fidelity wireframes let us test layout and flow before visual design. After two rounds, users completed the main tasks much faster."],
        images:["half","half"]},
      {title:"Visual design",
        body:["A warm, appetising colour palette, big food photography and clear typography. I built a component library in Figma — buttons, cards, inputs and states — so new screens stay consistent."],
        images:["full"]},
      {title:"Prototyping & testing",
        body:["I built a clickable prototype in Figma and tested it with real users over several rounds."],
        stats:[["15","participants"],["3","test rounds"],["90%","task success"]],
        quote:{text:"Finally I can see the full price before I pay. Ordering feels so much quicker.", by:"Test participant"}},
      {title:"Iterations",
        body:["Testing showed users missed the reorder button and wanted fees earlier. I moved reorder to the home screen and showed the total price on every restaurant card."],
        beforeAfter:true},
      {title:"Final design",
        list:["Home screen built around cravings and quick reorder","Clear restaurant cards with time, rating and total price","One-page checkout with saved payment","Live order tracking with rider location"],
        images:["half","half"]},
      {title:"Impact",
        body:["The redesign made ordering faster and clearer for customers and easier to build for the team."],
        stats:[["-35%","checkout steps"],["2 taps","to reorder"],["40+","reusable components"]]},
      {title:"Lessons learned",
        list:["Speed matters more than decoration when users are hungry","Showing prices early builds trust","Small usability tests early save big changes later","A design system speeds up the whole team"]},
      {title:"Next steps",
        list:["Personalised recommendations","Group ordering for friends and offices","Dark mode","Accessibility review"]}
    ]
  }
};
