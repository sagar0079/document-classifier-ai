# Frontend setup

The `src/` folder here contains the custom application code (component, service,
config, environments). Angular's own scaffolding files (`package.json`, `angular.json`,
`tsconfig*.json`) aren't included, since they're best generated fresh by the Angular CLI
for whatever version you have installed. To assemble a runnable project:

```bash
# 1. Scaffold a new standalone Angular app (choose "No" for SSR, "CSS" for styling)
npx @angular/cli@latest new document-classifier-ui --standalone --routing=false --style=css

# 2. Move into the new project
cd document-classifier-ui

# 3. Copy the contents of this frontend/src folder over the generated src folder,
#    overwriting main.ts, app.component.*, index.html, styles.css, and adding
#    app.config.ts, environments/, document-classifier/, and services/

# 4. Copy Dockerfile, nginx.conf.template, and .dockerignore into the project root too

# 5. Run it locally
ng serve
```

Then open http://localhost:4200 - make sure the backend is running on
http://localhost:8080 (or update `src/environments/environment.ts`).
