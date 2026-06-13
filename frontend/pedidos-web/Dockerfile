# ---- build ----
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
# Variáveis VITE_* são build-time (embutidas no bundle).
ARG VITE_API_URL=http://localhost:5005/api
ARG VITE_NOTIFICATIONS_URL=http://localhost:3001
ENV VITE_API_URL=$VITE_API_URL
ENV VITE_NOTIFICATIONS_URL=$VITE_NOTIFICATIONS_URL
RUN npm run build

# ---- serve ----
FROM nginx:alpine AS serve
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
