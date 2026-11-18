/* ======================================================
   SERVICE DETAILS — shown on service.html?s=<slug>
   Key = the service's slug (title in lowercase with dashes),
   e.g. "Website design" -> "website-design".

   summary:   longer text under the title
   metrics:   4 cards — Price comes from data.js (price field)
   included:  "What's included" cards
   steps:     "How it works" (4 steps)
   type:      which projects show in "Related work" (web | app | dash)

   !! Durations, deliverables and revisions are SUGGESTED
   !! defaults — change them to match how you really work.
   ====================================================== */
const SERVICE_ICONS = {
  compass:'<circle cx="12" cy="12" r="9"/><path d="m15.5 8.5-2 5-5 2 2-5z"/>',
  layout:'<rect x="3" y="4" width="18" height="16" rx="2"/><path d="M3 9h18M9 9v11"/>',
  phone:'<rect x="7" y="2.5" width="10" height="19" rx="2.5"/><path d="M11 18.5h2"/>',
  chart:'<path d="M4 20V10M10 20V4M16 20v-7M22 20H2"/>',
  pen:'<path d="M4 20h4L19 9l-4-4L4 16z"/><path d="M13.5 6.5l4 4"/>',
  palette:'<path d="M12 3a9 9 0 1 0 0 18c1.1 0 1.5-.8 1.5-1.6 0-1-.9-1.4-.9-2.4 0-.8.7-1.5 1.5-1.5H17a4 4 0 0 0 4-4c0-4.7-4-8.5-9-8.5z"/><circle cx="7.5" cy="11" r="1"/><circle cx="10" cy="7" r="1"/><circle cx="14.5" cy="7" r="1"/>',
  play:'<rect x="3" y="4" width="18" height="16" rx="2"/><path d="m10 9 5 3-5 3z"/>',
  file:'<path d="M14 3H6v18h12V7z"/><path d="M14 3v4h4M9 13h6M9 17h6"/>',
  users:'<circle cx="9" cy="8" r="3.5"/><path d="M2.5 20a6.5 6.5 0 0 1 13 0M16 4.5a3.5 3.5 0 0 1 0 7M18 14a6 6 0 0 1 3.5 6"/>',
  check:'<circle cx="12" cy="12" r="9"/><path d="m8 12 3 3 5-6"/>'
};

const SERVICE_DETAILS = {
  "website-design": {
    type: "web",
    summary: "Landing pages and full websites planned around what your visitors need to decide, then designed for desktop, tablet and phone.",
    metrics: [["Duration","1–3 weeks","clock"],["Deliverables","Figma files","grid"],["Revisions","Two rounds","check"]],
    included: [
      ["compass","Research and structure","A clear page plan based on your goals, users and competitors."],
      ["layout","Responsive page design","Every page designed for desktop, tablet and phone."],
      ["play","Clickable prototype","A Figma prototype you can click through before anything is built."],
      ["file","Developer handoff","Organised files, styles and components ready to build."]
    ],
    steps: [
      ["Discover","We talk about your goals, audience and what the site must achieve."],
      ["Structure","I plan the pages and content so every section has a purpose."],
      ["Design","High-quality designs for every screen size, with your feedback."],
      ["Handoff","Final files and support for your developer or Webflow build."]
    ]
  },
  "mobile-app-design": {
    type: "app",
    summary: "Clean, easy-to-use app screens for iOS and Android — from user flows and wireframes to a polished, clickable prototype.",
    metrics: [["Duration","2–4 weeks","clock"],["Deliverables","30+ screens","grid"],["Revisions","Two rounds","check"]],
    included: [
      ["users","User flows","Simple journeys for the tasks your users care about most."],
      ["phone","App UI design","Every screen and state designed for iOS and Android."],
      ["play","Interactive prototype","A clickable prototype for testing and investor demos."],
      ["grid","Component library","Reusable components so new screens stay consistent."]
    ],
    steps: [
      ["Discover","We define your users, their goals and the core features."],
      ["Wireframe","Low-fidelity flows to test the structure quickly."],
      ["Design","Polished UI with a clickable prototype for feedback."],
      ["Handoff","Organised Figma files and support for your developers."]
    ]
  },
  "dashboard-and-saas-ui": {
    type: "dash",
    summary: "Data-heavy products made simple. Dashboards and SaaS interfaces your users understand at first look and enjoy using every day.",
    metrics: [["Duration","2–4 weeks","clock"],["Deliverables","Design system","grid"],["Revisions","Two rounds","check"]],
    included: [
      ["chart","Dashboard design","Clear charts, tables and layouts that show what matters."],
      ["users","User flows","Smooth paths through complex tasks and settings."],
      ["grid","Design system","Components, states and rules your team can reuse."],
      ["file","Developer handoff","Specs and organised files that make building easier."]
    ],
    steps: [
      ["Audit","I review your product, data and the problems users face."],
      ["Structure","We decide what each screen must show and in what order."],
      ["Design","Clean, consistent UI built on a reusable component library."],
      ["Handoff","Files, components and support while your team builds."]
    ]
  },
  "brand-and-graphic-design": {
    type: "web",
    summary: "Logos, brand identity and marketing graphics that make your business look trustworthy and consistent everywhere.",
    metrics: [["Duration","1–2 weeks","clock"],["Deliverables","Logo + brand kit","grid"],["Revisions","Two rounds","check"]],
    included: [
      ["pen","Logo design","A distinctive logo with variations for every use."],
      ["palette","Colour and type","A palette and fonts that work on screen and in print."],
      ["file","Brand guidelines","A short, practical guide for using your brand."],
      ["layout","Marketing graphics","Social posts, flyers and banners in your new style."]
    ],
    steps: [
      ["Brief","We agree on your audience, values and the feeling you want."],
      ["Concepts","A few focused logo and style directions to choose from."],
      ["Refine","One direction, two rounds of changes, every detail polished."],
      ["Deliver","All files, formats and guidelines, ready to use."]
    ]
  }
};
