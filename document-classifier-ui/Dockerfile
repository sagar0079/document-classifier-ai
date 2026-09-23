# ---- Build stage ----
FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build -- --configuration production

# ---- Runtime stage ----
FROM nginx:alpine AS runtime

COPY nginx.conf.template /etc/nginx/templates/default.conf.template
# Angular CLI outputs to dist/<project-name>/browser - adjust the folder name below
# if you named your project something other than "document-classifier-ui"
COPY --from=build /app/dist/document-classifier-ui/browser /usr/share/nginx/html

ENV PORT=8080
EXPOSE 8080

CMD ["/bin/sh", "-c", "envsubst '$PORT' < /etc/nginx/templates/default.conf.template > /etc/nginx/conf.d/default.conf && nginx -g 'daemon off;'"]
