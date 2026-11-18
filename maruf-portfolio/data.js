/* ======================================================
   EDIT YOUR CONTENT HERE
   The first 3 projects in PROJECTS show on the home page.
   Every project shows on the Work page (work.html).
   ====================================================== */
const CONTACT_EMAIL = "hello@yourdomain.com"; // TODO: your email

// type: web | app | dash   ·   img: put file in /images and set e.g. "images/agency.jpg"
const PROJECTS = [
  {t:"Creative Agency Website", type:"web", cat:"Website", y:"2024", hl:"Landing + inner pages", sum:"Bold, modern website for a creative studio.", url:"https://dribbble.com/shots/25125559-Creative-Agency-Website", c1:"#dc9058", c2:"#6b3416", badge:"Featured"},
  {t:"Fintech App", type:"app", cat:"Mobile app", y:"2024", hl:"iOS & Android", sum:"Simple money app with clean dashboards.", url:"https://dribbble.com/shots/25053234-Fintech-App", c1:"#7f7bff", c2:"#3b2bd6"},
  {t:"AI Image Generator", type:"web", cat:"Website", y:"2024", hl:"SaaS landing", sum:"Landing page for an AI art product.", url:"https://dribbble.com/shots/24718465-Modern-AI-Image-Generator-Website", c1:"#a855f7", c2:"#4c1d95"},
  {t:"EV Vehicles Website", type:"web", cat:"Website", y:"2023", hl:"Product site", sum:"Electric vehicle brand website.", url:"https://dribbble.com/shots/22944003-EV-vehicles-website-UI-design", c1:"#4ade80", c2:"#0e7a52"},
  {t:"Fitness & Workout App", type:"app", cat:"Mobile app", y:"2023", hl:"Health", sum:"Workout tracking with clear progress.", url:"https://dribbble.com/shots/21767589-Fitness-Workout-Mobile-App", c1:"#bef264", c2:"#4d7c0f"},
  {t:"Sports Matches Dashboard", type:"dash", cat:"Dashboard", y:"2023", hl:"Live data", sum:"Scores, stats and fixtures in one view.", url:"https://dribbble.com/shots/21741384-Sports-Matchs-Dashboard", c1:"#38bdf8", c2:"#0b4f8a"},
  {t:"Food Delivery App", type:"app", cat:"Mobile app", y:"2023", hl:"Ordering flow", sum:"Fast ordering from menu to checkout.", url:"https://dribbble.com/shots/21541077-Food-delivery-UI-Design-Mobile-app", c1:"#fbbf24", c2:"#c2410c"},
  {t:"Crypto Dashboard", type:"dash", cat:"Dashboard", y:"2022", hl:"Fintech", sum:"Portfolio and market tracking.", url:"https://dribbble.com/shots/18393214-Crypto-Currency-Dashboard", c1:"#f7931a", c2:"#5a3300"},
  {t:"E-Commerce Web UI", type:"web", cat:"Website", y:"2022", hl:"Online store", sum:"Shop design focused on sales.", url:"https://dribbble.com/shots/19414020-E-Commerce-Web-UI-Design", c1:"#f472b6", c2:"#9d174d"},
  {t:"Travel Agency Landing", type:"web", cat:"Website", y:"2022", hl:"Booking", sum:"Inspiring travel landing page.", url:"https://dribbble.com/shots/18535523-Travel-Agency-Landing-Page-Design", c1:"#22d3ee", c2:"#0e5f74"},
  {t:"Game Landing Page", type:"web", cat:"Website", y:"2022", hl:"Gaming", sum:"High-energy page for a game launch.", url:"https://dribbble.com/shots/19848408-Game-Landing-Page-UI-Design", c1:"#ef4444", c2:"#5f0f0f"},
  {t:"Doctor Landing Page", type:"web", cat:"Website", y:"2022", hl:"Healthcare", sum:"Trustworthy clinic booking page.", url:"https://dribbble.com/shots/17730616-Doctor-Landing-Page", c1:"#60a5fa", c2:"#1e3a8a"}
];

const SERVICES = [
  {t:"Website design", s:"Landing pages and full websites that explain your offer fast and turn visitors into leads.", tags:["Landing pages","Agency sites","E-commerce"], icon:'<rect x="3" y="4" width="18" height="16" rx="2"/><path d="M3 9h18M7 6.5h.01M10 6.5h.01"/>'},
  {t:"Mobile app design", s:"Clean, easy app screens for iOS and Android, from wireframe to clickable prototype.", tags:["Fintech","Fitness","Food delivery"], icon:'<rect x="7" y="2.5" width="10" height="19" rx="2.5"/><path d="M11 18.5h2"/>'},
  {t:"Dashboard & SaaS UI", s:"Data-heavy screens made simple. Dashboards your users understand at first look.", tags:["Admin panels","Crypto","Analytics"], icon:'<path d="M4 20V10M10 20V4M16 20v-7M22 20H2"/>'},
  {t:"Brand & graphic design", s:"Logos, brand identity and marketing graphics that make your business look trustworthy.", tags:["Logo","Brand identity","Social media"], icon:'<path d="M12 3l2.6 5.6L20 9.5l-4 4 1 5.8-5-2.8-5 2.8 1-5.8-4-4 5.4-.9z"/>'}
];

// TODO: sample quotes — replace with real reviews before going live
const REVIEWS = [
  {q:"Maruf understood our idea quickly and delivered a clean, modern design. Great communication.", n:"Client name", r:"Founder", co:"Startup", a:"#dc9058", b:"#6b3416"},
  {q:"The app design was simple and beautiful. Our users loved the new flow.", n:"Client name", r:"Product manager", co:"Fintech", a:"#7f7bff", b:"#3b2bd6"},
  {q:"Fast delivery, helpful revisions and Figma files our developers could use right away.", n:"Client name", r:"CTO", co:"Agency", a:"#4ade80", b:"#0e9f6e"},
  {q:"Our landing page looks professional now and we get more enquiries every week.", n:"Client name", r:"Owner", co:"Local business", a:"#fbbf24", b:"#d97706"},
  {q:"Very detail-focused designer. The dashboard is much easier for our team to use.", n:"Client name", r:"Operations lead", co:"SaaS", a:"#38bdf8", b:"#0b4f8a"}
];

const FAQ = [
  ["Timeline","How long does a project take?","A landing page usually takes 3–7 days. A full website or app takes 2–4 weeks, depending on the number of screens."],
  ["Start","What do I need to get started?","Just your idea, your goals and any brand materials you have. If you don't have a brand yet, I can create one."],
  ["Build","Do you also build the website?","I focus on design and deliver clean Figma files ready for developers. I can also build on Webflow or Framer when needed."],
  ["Revisions","How many revisions do I get?","Every project includes revision rounds, so you are happy with the final result."],
  ["Payment","How do payments work?","You can hire me through Fiverr or directly. Direct projects usually start with a 50% deposit."]
];

const TOOLS = ["Figma","Adobe XD","Photoshop","Illustrator","FigJam","Webflow","Framer","Prototyping","Design systems"];
/* ====================================================== */
